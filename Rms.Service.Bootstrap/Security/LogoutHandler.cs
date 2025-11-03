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
﻿using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Handles user inactivity
/// </summary>
public class LogoutHandler
{
    private readonly UserService                _user;
    private readonly IOptions<SecuritySettings> _securitySettings;
    private readonly NavigationManager          _navigationManager;
    private readonly ILogger<LogoutHandler>     _log;
    private readonly IServiceProvider           _services;
    private          DateTime?                  _logoutAt;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="user"></param>
    /// <param name="securitySettings"></param>
    /// <param name="navigationManager"></param>
    /// <param name="logger"></param>
    /// <param name="services"></param>
    public LogoutHandler(
        UserService                user,
        IOptions<SecuritySettings> securitySettings,
        NavigationManager          navigationManager,
        ILogger<LogoutHandler>     logger,
        IServiceProvider           services
    )
    {
        _user              = user;
        _securitySettings  = securitySettings;
        _navigationManager = navigationManager;
        _log               = logger;
        _services          = services;
    }

    /// <summary>
    /// Call it in MainLayout.OnInitializedAsync.
    /// Requires auth-helper.js to be loaded.
    /// </summary>
    /// <param name="jsRuntime"></param>
    /// <returns></returns>
    public async Task OnAfterRenderAsync(IJSRuntime jsRuntime)
    {
        // https://stackoverflow.com/questions/73335150/logout-after-a-specific-idle-time-in-asp-net-core-identity
        await jsRuntime.InvokeVoidAsync("AuthHelper.initializeInactivityTimer", DotNetObjectReference.Create(this));
    }

    /// <summary>
    /// JS callback called every minute
    /// </summary>
    /// <param name="activityDetected"></param>
    /// <returns></returns>
    [JSInvokable]
    public async Task ActivityDetected(bool activityDetected)
    {
        if ( !_user.IsAuthenticated )
            return;

        if ( activityDetected )
        {
            _logoutAt = DateTime.Now + _securitySettings.Value.LogoffIdlePeriod;
            //_log.LogDebug($"ActivityDetected={activityDetected} Next logout time={_logoutAt:s} Checking tokens...");
        }
        
        if ( DateTime.Now > _logoutAt || ((await _user.GetTokens())?.IsRefreshTokenExpired ?? false))
        {
            _logoutAt = null;
            _log.LogDebug($"ActivityDetected={activityDetected} Logging out");
            _navigationManager.NavigateTo("logout", new NavigationOptions { ForceLoad = true });
            return;
        }

        // renew token even if there were no user activity
        // logout will handle 15 min user inactivity period
        var user  = _services.GetRequiredService<UserService>();
        var token = await user.GetTokens() ?? new UserTokens();
        if ( await TokenRefreshHelper.UpdateAccessTokenIfNeeded(_services, token) ?? false )
        {
            var tokenStore = _services.GetRequiredService<IServerSideTokenStore>();
            await tokenStore.StoreTokensAsync(user.GetUser(), token);
        }

    }
}