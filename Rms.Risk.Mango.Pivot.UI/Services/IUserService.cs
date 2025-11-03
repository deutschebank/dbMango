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
﻿using System.Security.Claims;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public interface IUserService
{
    /// <summary>
    /// Get currently logged in user identity
    /// </summary>
    /// <returns></returns>
    ClaimsPrincipal GetUser();

    /// <summary>
    /// Is user authenticated?
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get email
    /// </summary>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    string GetEmail();

    /// <summary>
    /// Get any user identity claim
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    string? Get(string claimType);
}


