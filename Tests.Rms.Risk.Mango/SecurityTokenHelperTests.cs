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
using System.Security.Claims;
using System.Text;
using Rms.Service.Bootstrap.Security;

namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class SecurityTokenHelperTests
{
    [Test]
    public void ParseClaimsFromJwt_WithBearerPrefixAndRoleArray_ReturnsExpectedClaims()
    {
        var jwt = CreateUnsignedToken("{\"sub\":\"user-1\",\"role\":[\"Admin\",\"Reader\"]}");

        var claims = TokenHelper.ParseClaimsFromJwt($"Bearer {jwt}");

        Assert.Multiple(() =>
        {
            Assert.IsTrue(claims.Any(x => x.Type == "sub" && x.Value == "user-1"));
            Assert.IsTrue(claims.Any(x => x.Type == ClaimTypes.Role && x.Value == "Admin"));
            Assert.IsTrue(claims.Any(x => x.Type == ClaimTypes.Role && x.Value == "Reader"));
            Assert.IsFalse(claims.Any(x => x.Type == "role"));
        });
    }

    [Test]
    public void ParseClaimsFromJwt_WithMalformedToken_ReturnsEmptyClaims()
    {
        var claims = TokenHelper.ParseClaimsFromJwt("Bearer not-a-jwt");

        Assert.That(claims, Is.Empty);
    }

    [Test]
    public void UpdateTokens_StoresProvidedIdToken()
    {
        var accessToken = CreateJwt(DateTime.UtcNow.AddMinutes(20), "access-subject");
        var refreshToken = CreateJwt(DateTime.UtcNow.AddMinutes(40), "refresh-subject");
        var idToken = CreateJwt(DateTime.UtcNow.AddMinutes(30), "id-subject");

        var userTokens = new UserTokens();
        userTokens.UpdateTokens(accessToken, refreshToken, idToken);

        Assert.Multiple(() =>
        {
            Assert.AreEqual(accessToken, userTokens.AccessToken);
            Assert.AreEqual(refreshToken, userTokens.RefreshToken);
            Assert.AreEqual(idToken, userTokens.IdToken);
            Assert.AreNotEqual(userTokens.AccessToken, userTokens.IdToken);
        });
    }

    private static string CreateJwt(DateTime expiresAtUtc, string subject)
    {
        var token = new JwtSecurityToken(
            claims: [new Claim("sub", subject)],
            expires: expiresAtUtc);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateUnsignedToken(string payloadJson)
        => $"{Base64UrlEncode("{\"alg\":\"none\",\"typ\":\"JWT\"}")}.{Base64UrlEncode(payloadJson)}.";

    private static string Base64UrlEncode(string value)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                  .TrimEnd('=')
                  .Replace('+', '-')
                  .Replace('/', '_');
}
