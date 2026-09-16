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
﻿using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Security.Claims;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// OpenIDConnect helpers
/// </summary>
internal static class OidcHelper
{
    private static readonly ILogger _log = Logging.Logging.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType ?? typeof(int));

    /// <summary>
    /// Configure JWTBearer authorization for Oidc for use with API servers.
    /// </summary>
    public static void ConfigureOidcJwtBearer(IOptions<SecuritySettings> settings, JwtBearerOptions x)
    {
        var oidcConfiguration = OidcConfiguration.Load(settings.Value);

        x.Configuration = oidcConfiguration;
        x.TokenValidationParameters = new()
        {
            ValidAudience = settings.Value.Oidc.ClientId,
            ValidAudiences = new [] {settings.Value.Oidc.ClientId}.Concat(settings.Value.Oidc.ValidClientIds)
        };
        x.Events = CreateJwtEventsHandler();
    }

    /// <summary>
    /// Configure OAuth2 to use Oidc. Use within web applications.
    /// </summary>
    public static void ConfigureOpenIdConnect(IOptions<SecuritySettings> settings, OpenIdConnectOptions options)
    {
        if ( string.IsNullOrWhiteSpace(settings.Value.Oidc.Secret) )
            throw new ApplicationException("ClientSecret is not provided. It required for Oidc");

        var oidcConfiguration = OidcConfiguration.Load(settings.Value);

        options.Configuration = oidcConfiguration;
        options.ClientId      = settings.Value.Oidc.ClientId; 
        options.ClientSecret  = settings.Value.Oidc.Secret;

        options.Authority                     = oidcConfiguration.Issuer;
        options.ResponseType                  = OpenIdConnectResponseType.Code;
        options.SaveTokens                    = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.UseTokenLifetime              = false; // sync cookie lifetime with token lifetime

        // https://stackoverflow.com/questions/66545492/the-cookie-aspnetcore-identity-application-has-set-samesite-none-and-must-a
        options.NonceCookie.SecurePolicy       = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite           = SameSiteMode.None;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite     = SameSiteMode.None;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("email");
        
        options.TokenValidationParameters = new()
        {
            NameClaimType = ClaimTypes.Email
        };

        options.Events = new()
        {
            OnRedirectToIdentityProvider = context =>
            {
                context.Properties.RedirectUri = NormalizeReturnUrl(
                    context.Properties.RedirectUri ?? $"{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}");

                var builder = new UriBuilder( context.ProtocolMessage.RedirectUri );

                if (!string.IsNullOrWhiteSpace(settings.Value.Oidc.ForceRedirectUrlProtocol))
                    builder.Scheme = settings.Value.Oidc.ForceRedirectUrlProtocol;
                if ( settings.Value.Oidc.ForceRedirectUrlPort != 0 )
                    builder.Port = settings.Value.Oidc.ForceRedirectUrlPort;

                context.ProtocolMessage.RedirectUri = builder.ToString();

                _log.LogDebug($"Redirecting OIDC login to RedirectUri={context.ProtocolMessage.RedirectUri} ReturnUri=\"{context.Properties.RedirectUri}\"");

                return Task.CompletedTask;
            },
            OnAccessDenied = context =>
            {
                context.HandleResponse();
                context.Response.Redirect("/");
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                context.HandleResponse();
                _log.LogError( context.Exception, "Authentication failed" );
                context.Response.Redirect("/login-failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var email = context.SecurityToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Email || x.Type == "email")?.Value;

                try
                {
                    var token = new UserTokens();
                    token.UpdateTokens( 
                        context.TokenEndpointResponse?.AccessToken  ?? throw new ApplicationException( "Access token is NULL" ),
                        context.TokenEndpointResponse?.RefreshToken ?? throw new ApplicationException( "Refresh token is NULL" ),
                        context.TokenEndpointResponse?.IdToken
                    );

                    var svc = context.HttpContext.RequestServices.GetRequiredService<IServerSideTokenStore>();
                    await svc.StoreTokensAsync( context.Principal!, token );

                    _log.LogDebug($"Received tokens for Email=\"{email}\" ExpireAt=\"{token.AccessTokenExpiresAt}\" AccessToken={token.AccessToken != null} RefreshToken={token.RefreshToken != null} IdToken={token.IdToken != null}");
                }
                catch( Exception e )
                {
                    context.HandleResponse();
                    _log.LogError( e, $"Authentication succeeded for {email}, but token storage failed" );
                    context.Response.Redirect("/login-failed");
                }
            },
            OnTicketReceived = context =>
            {
                context.ReturnUri = NormalizeReturnUrl(context.Properties?.RedirectUri ?? context.ReturnUri);
                return Task.CompletedTask;
            },
        };

        _log.LogInformation($"Configured Oidc on {settings.Value.Oidc.ConfigUrl} ClientId=\"{options.ClientId}\"");
    }

    private static JwtBearerEvents CreateJwtEventsHandler()
    {
        return new()
        {
            OnMessageReceived = context =>
            {
                _log.LogDebug( $"{context.Request.Method} {context.Request.Path} Request received" );
                return Task.CompletedTask;
            },

            OnTokenValidated = context =>
            {
                context.Request.Headers.TryGetValue( "ReqId", out var reqId );
                _log.LogDebug( $"{context.Request.Method} ReqId={reqId} {context.Request.Path} Token validated {context.SecurityToken.Id} ValidTo=\"{context.SecurityToken.ValidTo}\"" );
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = authenticationFailedContext =>
            {
                if (authenticationFailedContext.Exception.GetType() == typeof(SecurityTokenExpiredException))
                {
                    authenticationFailedContext.Response.Headers["Token-Expired"] = "true";
                }

                authenticationFailedContext.Request.Headers.TryGetValue("ReqId", out var reqId);

                _log.LogError( $"JWT Authentication failed Method={authenticationFailedContext.Request.Method} ReqId={reqId} Path=\"{authenticationFailedContext.Request.Path}\"", authenticationFailedContext.Exception);
                return Task.CompletedTask;
            },
            OnForbidden = forbiddenContext =>
            {
                forbiddenContext.Request.Headers.TryGetValue("ReqId",         out var reqId);
                forbiddenContext.Request.Headers.TryGetValue("Authorization", out var token);

                // do not log the token!!!
                var s = (string?)token;
                if ( s == null )
                    return Task.CompletedTask;

                var claims    = TokenHelper.ParseClaimsFromJwt(s);
                var claimsStr =string.Join(", ", claims.Select(claim => $"{claim.Subject?.Name}:{claim.Value}"));

                _log.LogError($"JWT Authentication forbidden Method={forbiddenContext.Request.Method}, ReqId={reqId}, Path=\"{forbiddenContext.Request.Path}\" Claims=\"{claimsStr}\"");
                return Task.CompletedTask;
            }
        };
    }

    /// <summary>
    /// https://stackoverflow.com/questions/72868249/how-to-handle-user-oidc-tokens-in-blazor-server-when-the-browser-is-refreshed-an
    /// </summary>
    public static void ConfigureCookieForOpenIdConnect(IOptions<SecuritySettings> settings, CookieAuthenticationOptions options)
    {
        var cookieName = settings.Value.Oidc.CookieName;
        if (string.IsNullOrWhiteSpace(cookieName))
            cookieName = $".{AppDomain.CurrentDomain.FriendlyName}.Cookies";

        options.Cookie.Name         = cookieName;
        options.Cookie.HttpOnly     = true;
        options.Cookie.IsEssential  = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.Events.OnValidatePrincipal = async context =>
        {
            var user = context.Principal;
            if ( user?.Identity == null || !user.Identity.IsAuthenticated )
                return;

            // get user's tokens from server side token store
            var tokenStore = context.HttpContext.RequestServices.GetRequiredService<IServerSideTokenStore>();
            var tokens     = await tokenStore.GetTokensAsync(user);
            if ( tokens?.AccessToken == null
             || tokens.IdToken == null
             || tokens.RefreshToken == null )
            {
                // if we lack either an access or refresh token,
                // then reject the Principal (forcing a round trip to the id server)
                context.RejectPrincipal();
                return;
            }

            // we have a custom API client that takes care of refreshing our tokens 
            // and storing them in ServerSideTokenStore, we call that here
            if ( await TokenRefreshHelper.UpdateAccessTokenIfNeeded(context.HttpContext.RequestServices, tokens, user) ?? false )
            {
                await tokenStore.StoreTokensAsync(user, tokens);
            }

            // check the tokens have been updated
            var newTokens = await tokenStore.GetTokensAsync(user);
            if ( newTokens?.IsAccessTokenExpired ?? true )
            {
                // if we lack an access token or it was not successfully renewed, 
                // then reject the Principal (forcing a round trip to the id server)
                context.RejectPrincipal();
            }
        };
    }

    private static string NormalizeReturnUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "/";

        if ( url.StartsWith("/login/", StringComparison.OrdinalIgnoreCase) )
        {
            var plainUrl = TryBase64Decode(Uri.UnescapeDataString(url["/login/".Length ..]));
            if ( !string.IsNullOrWhiteSpace(plainUrl) )
                url = plainUrl;
        }

        if (string.IsNullOrWhiteSpace(url)
         || url.Equals("/login", StringComparison.OrdinalIgnoreCase)
         || url.Equals("/signin-oidc", StringComparison.OrdinalIgnoreCase)
         || !IsLocalRedirectUrl(url))
        {
            return "/";
        }

        return url;
    }

    private static bool IsLocalRedirectUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url[0] != '/')
            return false;

        return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
    }

    private static string? TryBase64Decode(string base64EncodedData)
    {
        try
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
