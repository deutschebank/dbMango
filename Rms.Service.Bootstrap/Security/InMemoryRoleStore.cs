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
using Microsoft.AspNetCore.Identity;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Storage for created roles objects.
/// </summary>
internal class InMemoryRoleStore : IRoleClaimStore<IdentityRole>
{
    private static readonly List<IdentityRole> _roles      = [];
    private static          int                _nextRoleId = 1000;
    public void Dispose()
    {
    }

    public Task<IdentityResult> CreateAsync( IdentityRole role, CancellationToken cancellationToken )
    {
        role.Id = (_nextRoleId++).ToString();
        _roles.Add( role );
        return Task.FromResult( IdentityResult.Success );
    }

    public Task<IdentityResult> UpdateAsync( IdentityRole role, CancellationToken cancellationToken )
    {
        var r = _roles.FirstOrDefault( x => x.Id == role.Id || x.Name == role.Name );
        if ( r == null )
            return Task.FromResult( IdentityResult.Failed( new IdentityError { Code = "1", Description = "Role is not found"} ) );
        return Task.FromResult( IdentityResult.Success );
    }

    public Task<IdentityResult> DeleteAsync( IdentityRole role, CancellationToken cancellationToken )
    {
        var r = _roles.FirstOrDefault( x => x.Id == role.Id || x.Name == role.Name );
        if ( r == null )
            return Task.FromResult( IdentityResult.Failed( new IdentityError { Code = "1", Description = "Role is not found"} ) );
        _roles.Remove( r );
        return Task.FromResult( IdentityResult.Success );
    }

    public Task<string> GetRoleIdAsync( IdentityRole role, CancellationToken cancellationToken )
    {
        var r = _roles.FirstOrDefault( x => x.Name == role.Name );
        if ( r == null )
            return Task.FromResult( "" );
        return Task.FromResult( r.Id );
    }

    public Task<string?> GetRoleNameAsync( IdentityRole role, CancellationToken cancellationToken ) => Task.FromResult( role.Name );

    public Task SetRoleNameAsync( IdentityRole role, string? roleName, CancellationToken cancellationToken )
    {
        role.Name = roleName;
        return Task.FromResult( IdentityResult.Success );
    }

    public Task<string?> GetNormalizedRoleNameAsync( IdentityRole role, CancellationToken cancellationToken )                => Task.FromResult( role.Name?.ToUpper() );
    public Task SetNormalizedRoleNameAsync( IdentityRole role, string? normalizedName, CancellationToken cancellationToken ) => SetRoleNameAsync( role, normalizedName, cancellationToken );
    public Task<IdentityRole?> FindByIdAsync( string roleId, CancellationToken cancellationToken )                           => Task.FromResult(_roles.FirstOrDefault( x => x.Id == roleId ));
    public Task<IdentityRole?> FindByNameAsync( string normalizedRoleName, CancellationToken cancellationToken )             => Task.FromResult(_roles.FirstOrDefault( x => x.Name == normalizedRoleName ));
    public Task<IList<Claim>> GetClaimsAsync(IdentityRole role, CancellationToken cancellationToken = new())                 => Task.FromResult<IList<Claim>>(new List<Claim>());
    public Task AddClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = new())                   => throw new NotImplementedException();
    public Task RemoveClaimAsync(IdentityRole role, Claim claim, CancellationToken cancellationToken = new())                => throw new NotImplementedException();
}