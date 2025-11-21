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
﻿namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Middleware to provide currently logged in user. <see cref="UserService"/>
/// </summary>
public class UserServiceMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public UserServiceMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Invoke middleware chain
    /// </summary>
    /// <param name="context"></param>
    /// <param name="service"></param>
    /// <returns></returns>
    public async Task InvokeAsync(HttpContext context, UserService service)
    {
        service.SetUser(context.User);
        await _next(context);
    }
}
