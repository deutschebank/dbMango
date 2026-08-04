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
using System.Text;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Static class for JWT token-related stuff
/// </summary>
public static class TokenHelper
{
    private const string ClaimTypeRole = "role";

    /// <summary>
    /// Parse JSW token and extract claims from it
    /// </summary>
    /// <param name="jwt"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static IReadOnlyCollection<Claim> ParseClaimsFromJwt(string jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
            return Array.Empty<Claim>();

        var keyValuePairs = ParseJWT(jwt);
        if ( keyValuePairs == null )
            return Array.Empty<Claim>();

        var claims = new List<Claim>();

        if ( keyValuePairs.Remove(ClaimTypeRole, out var rolesObj) )
        {
            var roles = rolesObj?.ToString();
            if (roles != null)
            {
                if (roles.Trim().StartsWith("["))
                {
                    var parsedRoles = JsonSerializer.Deserialize<string[]>(roles) ?? [];
                    claims.AddRange(parsedRoles.Select(parsedRole => new Claim(ClaimTypes.Role, parsedRole)));
                }
                else
                {
                    claims.Add(new(ClaimTypes.Role, roles));
                }
            }
        }

        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString()! )));

        return claims;
    }

    private static Dictionary<string, object>? ParseJWT(string jwt)
    {
        jwt = jwt.Trim();
        if ( jwt.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) )
            jwt = jwt["Bearer ".Length..].Trim();

        var parts = jwt.Split('.');
        if ( parts.Length < 2 )
            return null;

        try
        {
            var json = Encoding.UTF8.GetString(ParseBase64WithoutPadding(parts[1]));
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}