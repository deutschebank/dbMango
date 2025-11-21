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
using Microsoft.AspNetCore.Authorization;
using Novell.Directory.Ldap;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Services.Context;
using Rms.Service.Bootstrap.Security;
// ReSharper disable InconsistentNaming

namespace Rms.Risk.Mango.Services.Security;

public class AdminAccessRequirement : IAuthorizationRequirement;
public class ReadAccessRequirement  : IAuthorizationRequirement;
public class WriteAccessRequirement : IAuthorizationRequirement;

public class AdminAuthorizationHandler(IDatabaseConfigurationService _databases, LdapChecker _ldap) 
    : AuthorizationHandler<AdminAccessRequirement, string>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminAccessRequirement      requirement,
        string /*database name*/    resource)
    {
        using var cts = new CancellationTokenSource(DatabaseConfigurationService.DefaultTimeout);
        var error = await HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.Admin,
            cts.Token
            );

        if (error == null)
            context.Succeed(requirement);
        else
            context.Fail(new(this, error));
    }
        

    public static async Task<string?> HandleRequirement(
        IDatabaseConfigurationService config,
        LdapChecker               ldap,
        ClaimsPrincipal user,
        string database,
        Func<DatabasesConfig.DatabaseConfig.LdapGroups, string> getAcl,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(database))
            return null;

        if (!config.Databases.TryGetValue(database, out var conf))
            return $"Database {database} is not configured";

        var requiredGroup = getAcl(conf.Groups);

        if (string.IsNullOrWhiteSpace(requiredGroup))
            return null;

        try
        {
            var success = await ldap.IsGroupMember(UserService.GetEmail(user), requiredGroup, token);

            return success
                    ? null
                    : $"To access database {database} user should be a member of the group: {requiredGroup}"
                ;
        }
        catch (LdapException ex)
        {
            return $"To access database {database} user should be a member of the group: {requiredGroup}. Exception: {ex.LdapErrorMessage}";
        }
        catch (Exception ex)
        {
            return $"To access database {database} user should be a member of the group: {requiredGroup}. Exception: {ex.Message}";
        }
    }
}

public class ReadWriteAuthorizationHandler(IDatabaseConfigurationService _databases, LdapChecker _ldap)
    : AuthorizationHandler<WriteAccessRequirement, string>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        WriteAccessRequirement      requirement,
        string /*database name*/    resource)
    {
        using var cts = new CancellationTokenSource(DatabaseConfigurationService.DefaultTimeout);
        var error = await AdminAuthorizationHandler.HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.Admin,
            cts.Token
        );

        if (error == null)
        {
            context.Succeed(requirement);
            return;
        }
        
        error = await AdminAuthorizationHandler.HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.ReadWrite,
            cts.Token
        );

        if (error == null)
            context.Succeed(requirement);
        else
            context.Fail(new(this, error));
    }
}

public class ReadOnlyAuthorizationHandler(IDatabaseConfigurationService _databases, LdapChecker _ldap)
    : AuthorizationHandler<ReadAccessRequirement, string>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ReadAccessRequirement       requirement,
        string /*database name*/    resource)
    {
        using var cts = new CancellationTokenSource(DatabaseConfigurationService.DefaultTimeout);

        var error = await AdminAuthorizationHandler.HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.ReadWrite, 
            cts.Token);

        if (error == null)
        {
            context.Succeed(requirement);
            return;
        }

        error = await AdminAuthorizationHandler.HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.Admin,
            cts.Token
        );

        if (error == null)
        {
            context.Succeed(requirement);
            return;
        }
        
        error = await AdminAuthorizationHandler.HandleRequirement(
            _databases, 
            _ldap, 
            context.User, 
            resource, 
            x => x.ReadOnly,
            cts.Token
        );

        if (error == null)
            context.Succeed(requirement);
        else
            context.Fail(new(this, error));
    }
}

