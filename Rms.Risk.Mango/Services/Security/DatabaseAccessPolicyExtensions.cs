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
﻿using Microsoft.AspNetCore.Authorization;

namespace Rms.Risk.Mango.Services.Security;

public static class DatabaseAccessPolicyExtensions
{
    public const string AdminAccessPolicy = "AdminAccess";
    public const string ReadAccessPolicy  = "ReadAccess";
    public const string WriteAccessPolicy = "WriteAccess";

    public static IServiceCollection AddMongoDbAccess(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
            {
                options.AddPolicy(AdminAccessPolicy, policy => policy.Requirements.Add(new AdminAccessRequirement()));
                options.AddPolicy(ReadAccessPolicy,  policy => policy.Requirements.Add(new ReadAccessRequirement()));
                options.AddPolicy(WriteAccessPolicy, policy => policy.Requirements.Add(new WriteAccessRequirement()));
            })
           .AddSingleton<IAuthorizationHandler,AdminAuthorizationHandler>()
           .AddSingleton<IAuthorizationHandler,ReadOnlyAuthorizationHandler>()
           .AddSingleton<IAuthorizationHandler,ReadWriteAuthorizationHandler>()
            ;

        return services;
    }

}