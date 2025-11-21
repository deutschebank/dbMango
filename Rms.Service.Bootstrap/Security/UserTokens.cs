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
﻿using System.IdentityModel.Tokens.Jwt;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Currently logged in user OAuth2 user tokens
/// </summary>
public class UserTokens
{
    private readonly Lock  _syncObject = new();

    /// <summary>
    /// Thread safely update tokens
    /// </summary>
    /// <returns>True if tokens were updated</returns>
    public bool UpdateTokens( string? accessToken, string? refreshToken, string? idToken = null )
    {
        lock ( _syncObject )
        {
            // either reset token or replace it if expired
            // do not replace newer token with the older one

            var updated = false;
            if ( accessToken == null || AccessToken == null|| AccessTokenExpiresAt < GetExpiry( accessToken ) )
            {
                AccessToken = accessToken;
                updated = true;
            }

            if ( refreshToken == null || RefreshToken == null || RefreshTokenExpiresAt < GetExpiry( refreshToken ) )
            {
                RefreshToken = refreshToken;
                updated      = true;

            }

            if ( idToken == null || IdToken == null || IdTokenExpiresAt < GetExpiry( idToken ) )
            {
                IdToken = accessToken;
                updated = true;

            }

            return updated;
        }
    }

    private static DateTime GetExpiry( string token )
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var secToken     = tokenHandler.ReadToken(token);
        return DateTime.SpecifyKind( secToken.ValidTo, DateTimeKind.Utc );
    }

    /// <summary>
    /// Access token
    /// </summary>
    public string? AccessToken
    {
        get;
        private set
        {
            field = value;
            if (field == null)
            {
                AccessTokenExpiresAt = null;
                return;
            }

            AccessTokenExpiresAt = GetExpiry(field);
        }
    }

    /// <summary>
    /// Id token
    /// </summary>
    public string? IdToken
    {
        get;
        private set
        {
            field = value;
            if (field == null)
            {
                IdTokenExpiresAt = null;
                return;
            }

            IdTokenExpiresAt = GetExpiry(field);
        }
    }


    /// <summary>
    /// Refresh token
    /// </summary>
    public string? RefreshToken
    {
        get;
        private set
        {
            field = value;
            if (field == null)
            {
                RefreshTokenExpiresAt = null;
                return;
            }

            RefreshTokenExpiresAt = GetExpiry(field);
        }
    }

    /// <summary>
    /// Forge token
    /// </summary>
    public string? ForgeToken
    {
        get;
        set
        {
            field = value;
            if (field == null)
            {
                ForgeTokenExpiresAt = null;
                return;
            }

            ForgeTokenExpiresAt = GetExpiry(field);
        }
    }

    /// <summary>
    /// Access token expiration period UTC
    /// </summary>
    public DateTime? AccessTokenExpiresAt { get; private set; }
    /// <summary>
    /// True if access token is expired
    /// </summary>
    public bool IsAccessTokenExpired => AccessTokenExpiresAt == null || AccessTokenExpiresAt <= DateTime.UtcNow;
    /// <summary>
    /// Refresh token expiration period UTC
    /// </summary>
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    /// <summary>
    /// Forge token expiration period UTC
    /// </summary>
    public DateTime? ForgeTokenExpiresAt { get; private set; }
    /// <summary>
    /// True if refresh token is expired
    /// </summary>
    public bool IsRefreshTokenExpired => RefreshTokenExpiresAt == null || RefreshTokenExpiresAt <= DateTime.UtcNow;
    /// <summary>
    /// True if Forge token is expired
    /// </summary>
    public bool IsForgeTokenExpired => ForgeTokenExpiresAt == null || ForgeTokenExpiresAt <= DateTime.UtcNow;
    /// <summary>
    /// Id token expiration period UTC
    /// </summary>
    public DateTime? IdTokenExpiresAt { get; private set; }
    /// <summary>
    /// True if Id token is expired
    /// </summary>
    public bool IsIdTokenExpired => IdTokenExpiresAt == null || IdTokenExpiresAt <= DateTime.UtcNow;

    /// <summary>
    /// Reset everything
    /// </summary>
    public void Clear()
    {
        AccessToken           = null;
        RefreshToken          = null;
        IdToken               = null;
        ForgeToken            = null;
        AccessTokenExpiresAt  = null;
        RefreshTokenExpiresAt = null;
        IdTokenExpiresAt      = null;
        ForgeTokenExpiresAt   = null;
    }

    /// <summary>
    /// Clear only expired tokens
    /// </summary>
    /// <returns>True if some tokens were removed</returns>
    public bool ClearExpired()
    {
        var updated = false;

        if ( IsAccessTokenExpired )
        {
            AccessToken          = null;
            AccessTokenExpiresAt = null;
            updated              = true;
        }

        if ( IsRefreshTokenExpired )
        {
            RefreshToken          = null;
            RefreshTokenExpiresAt = null;
            updated               = true;
        }

        if ( IsIdTokenExpired )
        {
            IdToken          = null;
            IdTokenExpiresAt = null;
            updated          = true;
        }

        if ( IsForgeTokenExpired )
        {
            ForgeToken          = null;
            ForgeTokenExpiresAt = null;
            updated             = true;
        }

        return updated;
    }

}