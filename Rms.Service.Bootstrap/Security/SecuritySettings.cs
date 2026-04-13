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
﻿using System.Diagnostics.CodeAnalysis;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// Settings for Oidc
/// </summary>
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
public class OidcSettings
{
    /// <summary>
    /// Client ID (application ID)
    /// </summary>
    public string ClientId { get; set; } = "";
    /// <summary>
    /// Additional ClientIds to accept while validating JWTs.
    /// Works only when user access token passed thru the request headers.
    /// I.e. for use withing API servers.
    /// </summary>
    public string[] ValidClientIds { get; set; } = [];
    /// <summary>
    /// Application secret
    /// </summary>
    public string Secret    { get; set; } = "";
    /// <summary>
    /// Well-known OAuth configuration URL
    /// If <see cref="ConfigCacheFile"/> supplied the config will be cached there.
    /// </summary>
    public string ConfigUrl { get;        set; } = "";
    /// <summary>
    /// If configuration can't be loaded from <see cref="ConfigUrl"/> it will be loaded from this
    /// file instead (if exists).
    /// </summary>
    public string? ConfigCacheFile { get; set; }
    /// <summary>
    /// Force different protocol on redirection URL. Useful if you are running in Docker container
    /// under service mesh where SSL termination happening on the Egress gateway.
    /// </summary>
    public string? ForceRedirectUrlProtocol { get; set; }
    /// <summary>
    /// Force different port on redirection URL. Useful if you are running in Docker container
    /// under service mesh where SSL termination happening on the Egress gateway.
    /// </summary>
    public int ForceRedirectUrlPort { get; set; }

    /// <summary>
    /// Optional authentication cookie name.
    /// Set this to avoid collisions with other apps hosted on the same domain.
    /// </summary>
    public string? CookieName { get; set; }
}


/// <summary>
/// Ldap settings
/// </summary>
public class LdapSettings
{
    /// <summary>
    /// AD server URL in form of ldaps://xxx.com
    /// </summary>
    public string Url { get; set; } = "";

    public string                     Username         { get; set; } = "";
    public string                     Password         { get; set; } = "";
    public string                     EntryPoint       { get; set; } = "";
    public Dictionary<string, string> RoleGroupMapping { get; set; } = new();
    public string[]                   IgnoreAccounts   { get; set; } = [];
}

/// <summary>
/// Security related settings
/// </summary>
[SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
public class SecuritySettings
{
    /// <summary>
    /// X.509 server (my) certificate file name.
    /// If empty no certificate will be used for server (http server in a service mesh).
    /// </summary>
    public string CertificateFileName { get; set; } = "";
    /// <summary>
    /// X.509 server (my) certificate password.
    /// If empty no certificate will be used for server (http server in a service mesh).
    /// </summary>
    public string CertificatePassword { get; set; } = "";

    /// <summary>
    /// Predefined roles definition. This can be loaded from dbEntitlements
    /// </summary>
    public Dictionary<string, string[]> Roles  { get; set; } = new();
    
    /// <summary>
    /// Secret to be used to issue JWT tokens.
    /// This property must be either encrypted or generated at the startup.
    /// </summary>
    public string Secret { get; set; } = "";

    /// <summary>
    /// Inactive user will be logged off after this time span
    /// </summary>
    public TimeSpan LogoffIdlePeriod { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Configuration for Oidc. <see cref="OidcSettings"/>
    /// </summary>
    public OidcSettings Oidc { get; set; } = new ();

    /// <summary>
    /// If certificate-based authentication is in use issuer with this name
    /// must be present in the certificate chain.
    /// </summary>
    public HashSet<string> CASubjectToAccept   { get; set; } = [];

    /// <summary>
    /// Client certificate CN to be access granted.
    /// </summary>
    public HashSet<string> ClientCertWhiteList { get; set; } = [];

    /// <summary>
    /// LDAP settings
    /// </summary>
    public LdapSettings Ldap                   { get; set; } = new();
}