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
﻿using System.Collections.Concurrent;
using System.Security.Claims;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Server-side authentication storage.
/// See https://stackoverflow.com/questions/72868249/how-to-handle-user-oidc-tokens-in-blazor-server-when-the-browser-is-refreshed-an
/// </summary>
public interface IServerSideTokenStore
{
    /// <summary>
    /// Remove all tokens
    /// </summary>
    Task                ClearTokensAsync(ClaimsPrincipal principal);
    /// <summary>
    /// Get tokens by user claim "sub" (i.e. subject)
    /// </summary>
    Task<UserTokens?> GetTokensAsync(ClaimsPrincipal principal);
    /// <summary>
    /// Store user tokens by user claim "sub" (i.e. subject)
    /// </summary>
    Task                StoreTokensAsync(ClaimsPrincipal principal, UserTokens userTokens);
    /// <summary>
    /// Extract user ID (normally email) from user claims.
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    string? GetId(ClaimsPrincipal principal);
}

internal class ServerSideTokenStore : IServerSideTokenStore
{
    private readonly ConcurrentDictionary<string, UserTokens> _userTokenProviders = new();

    public Task ClearTokensAsync(ClaimsPrincipal principal)
    {
        var userSub = GetId(principal);
        if (userSub == null) 
            return Task.CompletedTask;
            
        _userTokenProviders.TryRemove(userSub, out _);
        return Task.CompletedTask;
    }

    public Task<UserTokens?> GetTokensAsync(ClaimsPrincipal principal)
    {
        var userSub = GetId(principal);
        if (userSub == null) 
            return Task.FromResult((UserTokens?)null);

        if ( !_userTokenProviders.TryGetValue(userSub, out var value) )
            return Task.FromResult((UserTokens?)null);

        value.ClearExpired();
        if ( !HasTokens(value) )
        {
            _userTokenProviders.TryRemove(userSub, out _);
            return Task.FromResult((UserTokens?)null);
        }

        return Task.FromResult<UserTokens?>(value);
    }

    public Task StoreTokensAsync(ClaimsPrincipal principal, UserTokens userTokens)
    {
        var userSub = GetId(principal);
        if (userSub == null) 
            return Task.CompletedTask;

        _userTokenProviders[userSub] = userTokens;
        return Task.CompletedTask;
    }

    private static string? Get(ClaimsPrincipal principal, string claimType)
        => principal.Identities
                    .SelectMany(x => x.Claims)
                    .FirstOrDefault(x => x.Type == claimType && !string.IsNullOrWhiteSpace(x.Value))
                   ?.Value;

    public string? GetId(ClaimsPrincipal principal)
        => Get(principal, "sub")
         ?? Get(principal, ClaimTypes.NameIdentifier)
         ?? Get(principal, "usersub")
         ?? Get(principal, ClaimTypes.Email)
         ?? Get(principal, "email")
         ?? Get(principal, ClaimTypes.Name)
         ;

    private static bool HasTokens(UserTokens userTokens)
        => userTokens.AccessToken != null
        || userTokens.RefreshToken != null
        || userTokens.IdToken != null
        || userTokens.ForgeToken != null;
}
