/* 
 *                                dbMango
 *
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Rms.Service.Bootstrap.Security;
internal static class TokenRefreshHelper
{
    private static readonly ILogger _log = Logging.Logging.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType ?? typeof(int));

    public static int OidcTokenRefreshGapMinutes = 4;

    private static string? _clientId;
    private static string? _secret;

    /// <summary>
    /// Renew AccessToken using RefreshToken
    /// https://stackoverflow.com/questions/60858985/addopenidconnect-and-refresh-tokens-in-asp-net-core
    /// </summary>
    /// <returns>true if tokens were updated</returns>
    public static async Task<bool?> UpdateAccessTokenIfNeeded(
        IServiceProvider services,
        UserTokens userTokens,
        ClaimsPrincipal? principal = null
    )
    {
        if ( userTokens.AccessToken == null )
            return null;

        InitClientId( services );

        var email = GetUserEmail( services, principal );

        var needToRefresh = NeedToRefresh( userTokens, email );
        if ( needToRefresh != true ) 
            return needToRefresh;

        return await TryRefreshTokens( userTokens, email);
    }

    /// <summary>
    /// Check if token needs to be updated
    /// </summary>
    /// <returns>
    /// true - need to refresh token now
    /// false - token needs to be refreshed, but it's not possible (refresh token expired)
    /// null - there is no need to refresh token
    /// </returns>
    private static bool? NeedToRefresh( UserTokens userTokens, string? email )
    {
        if ( userTokens is { RefreshToken: not null, IsRefreshTokenExpired: true } )
        {
            //_log.LogDebug( $"Refresh token already expired for Email=\"{email}\" at Expiration=\"{userTokens.RefreshTokenExpiresAt:G}\", Now=\"{DateTime.UtcNow:G}\". No refresh attempt made." );
            userTokens.Clear();

            return false;
        }

        var expiration = userTokens.AccessTokenExpiresAt!;

        var gap = TimeSpan.FromMinutes( OidcTokenRefreshGapMinutes );

        // no need to update yet
        if ( expiration > DateTime.UtcNow + gap )
        {
            //_log.LogDebug($"Access token for Email=\"{email}\" expires at {expiration:G}, now is {DateTime.UtcNow:G}. There is plenty of time.");
            return null;
        }

        //_log.LogDebug( userTokens.IsAccessTokenExpired
        //                ? $"Access token for Email=\"{email}\" already expired at Expiration=\"{expiration:G}\", Now=\"{DateTime.UtcNow:G}\". Refreshing."
        //                : $"Access token for Email=\"{email}\" expires at Expiration=\"{expiration:G}, Now=\"{DateTime.UtcNow:G}\". Condition={expiration > DateTime.UtcNow + gap} Refreshing." );


        return true;
    }

    private static readonly SemaphoreSlim _globalLock = new(1, 1);

    private static async Task<bool?> TryRefreshTokens( UserTokens userTokens, string? email )
    {
        await _globalLock.WaitAsync();
        try
        {
            // second check in case token was already updated in another thread
            var needToRefresh = NeedToRefresh( userTokens, email );
            if ( needToRefresh != true )
            {
                //_log.LogDebug( $"Access token for Email=\"{email}\" renewed in different thread new Expiration=\"{userTokens.AccessTokenExpiresAt:G}\", Now=\"{DateTime.UtcNow:G}\"" );
                return needToRefresh;
            }

            return await TryRefreshTokensUnsafe( userTokens, email );
        }
        finally
        {
            _globalLock.Release();
        }
    }

    private static async Task<bool?> TryRefreshTokensUnsafe( UserTokens userTokens, string? email )
    {
        if ( _clientId == null || _secret == null )
            throw new ApplicationException( "Oidc is not configured" );

        var tokens = await RefreshTokens( _clientId, _secret, userTokens.RefreshToken!, email );

        if ( tokens != null )
        {
            UpdateTokenProvider( userTokens, tokens, email );
            return true;
        }

        _log.LogDebug( $"Access token for Email=\"{email}\" expired and revoked." );
        userTokens.Clear();

        return false;
    }

    private static void InitClientId( IServiceProvider services )
    {
        if ( _clientId == null || _secret == null )
        {
            var settings = services.GetRequiredService<IOptions<SecuritySettings>>();

            var pm = services.GetRequiredService<IPasswordManager>();
            _secret   = pm.DecryptPassword( settings.Value.Oidc.Secret );
            _clientId = settings.Value.Oidc.ClientId;
        }
    }

    private static string? GetUserEmail(IServiceProvider services, ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            var user = services.GetRequiredService<UserService>();
            principal = user.GetUser();
        }

        var email = Get(principal, ClaimTypes.Email) ?? Get(principal, "email");

        return email;
    }

    private static string? Get(ClaimsPrincipal principal, string claimType)
        => principal.Identities
                    .SelectMany(x => x.Claims)
                    .FirstOrDefault(x => x.Type == claimType && !string.IsNullOrWhiteSpace(x.Value))
                   ?.Value;

    private static async Task<JsonNode?> RefreshTokens(string clientId, string clientSecret, string refreshToken, string? email)
    {
        if (OidcConfiguration.Configuration == null)
            return null;

        var tokenEndpoint = OidcConfiguration.Configuration.TokenEndpoint;

        var requestData = new[]
        {
            new KeyValuePair<string, string>("client_id",     clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("grant_type",    "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken),
        };

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("accept", "application/json");

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestData));
        if (!response.IsSuccessStatusCode)
        {
            string data;
            try
            {
                data = await response.Content.ReadAsStringAsync();
            }
            catch( Exception e )
            {
                data = e.Message;
            }
            _log.LogError( $"Refresh token request unsuccessful for Email=\"{email}\": {response.StatusCode}\n\t{data}" );
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(json)?.AsObject();
    }

    private static void UpdateTokenProvider( UserTokens userTokens, JsonNode tokens, string? email )
    {
        var accessToken  = tokens[OpenIdConnectParameterNames.AccessToken]?.ToString();
        var refreshToken = tokens[OpenIdConnectParameterNames.RefreshToken]?.ToString();
        var idToken      = tokens[OpenIdConnectParameterNames.IdToken]?.ToString();

        if ( accessToken == null || refreshToken == null )
        {
            if ( userTokens.ClearExpired() )
            {
                //_log.LogDebug($"Expired token(s) were revoked. Email=\"{email}\"\n"+
                //           $"\tAccessTokenExpiresAt=\"{userTokens.AccessTokenExpiresAt:G}\" AccessTokenExpired={userTokens.IsAccessTokenExpired}\n" +
                //           $"\tRefreshTokenExpiresAt=\"{userTokens.RefreshTokenExpiresAt:G}\" RefreshTokenExpired={userTokens.IsRefreshTokenExpired}\n" +
                //           $"\tNow=\"{DateTime.UtcNow:G}\" (UTC time)");
            }
            return;
        }

        userTokens.UpdateTokens( accessToken, refreshToken, idToken );

        //_log.LogDebug($"Tokens updated for Email=\"{email}\" AccessTokenExpiresAt=\"{userTokens.AccessTokenExpiresAt:G}\" " +
        //           $"AccessTokenExpired={userTokens.IsAccessTokenExpired} Now=\"{DateTime.UtcNow:G}\" (UTC time)");
    }
}