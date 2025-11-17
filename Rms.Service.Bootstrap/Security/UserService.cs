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
﻿using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Security.Claims;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Service class to provide currently logged user.
/// For use within Blazor server-side web applications
/// </summary>
public class UserService
{
    private readonly IServerSideTokenStore _tokenStore;
    private          ClaimsPrincipal       _currentUser = new(new ClaimsIdentity());

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="tokenStore"></param>
    public UserService(IServerSideTokenStore tokenStore)
    {
        _tokenStore = tokenStore;
    }

    /// <summary>
    /// Get currently logged in user identity
    /// </summary>
    /// <returns></returns>
    public ClaimsPrincipal GetUser()
    {
        return _currentUser;
    }

    /// <summary>
    /// Is user authenticated?
    /// </summary>
    public bool IsAuthenticated => GetEmail() != string.Empty;

    ///// <summary>
    ///// User Id (can be a GUID!)
    ///// </summary>
    ///// <returns></returns>
    //public string GetId()    => Get(ClaimTypes.NameIdentifier) ?? GetEmail();

    /// <summary>
    /// Get email
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public string GetEmail() => Get(ClaimTypes.Email) ?? "";

    /// <summary>
    /// Get email
    /// </summary>
    public static string GetEmail(ClaimsPrincipal user) => Get(user, ClaimTypes.Email) ?? "";

    ///// <summary>
    ///// Get username
    ///// </summary>
    ///// <returns></returns>
    //public string GetName()
    //{
    //    var surname = Get(ClaimTypes.Surname);
    //    var givenName = Get(ClaimTypes.GivenName);

    //    if ( surname != null && givenName != null )
    //        return $"{givenName} {surname}";

    //    return Get(ClaimTypes.Name) ?? Get(ClaimTypes.Email) ?? "";
    //}

    /// <summary>
    /// Get any user identity claim
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public string? Get(string claimType) => Get(_currentUser, claimType);

    /// <summary>
    /// Get any user identity claim
    /// </summary>
    public static string? Get(ClaimsPrincipal user, string claimType) =>
        user.Identities
           .SelectMany(x => x.Claims)
           .FirstOrDefault(x => x.Type == claimType && !string.IsNullOrWhiteSpace(x.Value))
          ?.Value;

    internal void SetUser(ClaimsPrincipal user)
    {
        _currentUser = user;
    }

    /// <summary>
    /// Get access tokens
    /// </summary>
    /// <returns></returns>
    public async Task<UserTokens?> GetTokens() => await _tokenStore.GetTokensAsync(GetUser());
}

internal sealed class UserCircuitHandler(
    AuthenticationStateProvider authenticationStateProvider,
    UserService                 userService)
    : CircuitHandler, IDisposable
{
    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        authenticationStateProvider.AuthenticationStateChanged += AuthenticationChanged;
        return base.OnCircuitOpenedAsync(circuit, cancellationToken);
    }

    private void AuthenticationChanged(Task<AuthenticationState> task)
    {
        _ = UpdateAuthentication(task);

        async Task UpdateAuthentication(Task<AuthenticationState> x)
        {
            try
            {
                var state = await x;
                userService.SetUser(state.User);
            }
            catch
            {
                // ignore all errors
            }
        }
    }

    public override async Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        userService.SetUser(state.User);
    }

    public void Dispose()
    {
        authenticationStateProvider.AuthenticationStateChanged -= AuthenticationChanged;
    }

}