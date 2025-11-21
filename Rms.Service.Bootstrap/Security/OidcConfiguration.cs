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
﻿using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Reflection;

namespace Rms.Service.Bootstrap.Security;

/// <summary>
/// OpenIdConnect configuration loader
/// </summary>
internal static class OidcConfiguration
{
    private static readonly ILogger _log = Logging.Logging.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType ?? typeof(int));
    private static readonly Lock _lock = new();

    /// <summary>
    /// /.well-known/openid-configuration
    /// </summary>
    private const string ConfigurationSegment = "/.well-known/openid-configuration";


    /// <summary>
    /// Loaded Oidc configuration to use
    /// </summary>
    internal static OpenIdConnectConfiguration? Configuration { get; private set; }

    public static OpenIdConnectConfiguration Load(SecuritySettings settings)
    {
        if ( Configuration != null )
            return Configuration;

        lock ( _lock )
        {
            if ( Configuration != null )
                return Configuration;

            LoadOidcConfigurationInternal(settings);

            return Configuration!;
        }
    }

    private static void LoadOidcConfigurationInternal(SecuritySettings settings)
    {
        var url = settings.Oidc.ConfigUrl + ConfigurationSegment;
        
        try
        {
            _log.LogTrace($"Loading Oidc configuration from {url}");
        
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(url, new OpenIdConnectConfigurationRetriever());
            Configuration = configManager.GetConfigurationAsync().Result;

            _log.LogDebug($"Oidc configuration loaded from {url}");
            SaveOidcConfigurationToCache(settings);
        }
        catch (Exception e)
        {
            _log.LogDebug($"Can't load Oidc configuration from {url}\n{e}");

            var cacheFile = GetCacheFileName(settings);
            if ( settings.Oidc.ConfigCacheFile == null || !File.Exists( cacheFile))
                throw new ApplicationException($"Can't load Oidc configuration from {url}", e);

            var json = File.ReadAllText(cacheFile);
            Configuration = new(json);

            _log.LogWarning($"Oidc configuration loaded from {cacheFile}");
        }
    }

    private static void SaveOidcConfigurationToCache(SecuritySettings settings)
    {
        var cacheFile = GetCacheFileName(settings);

        if ( cacheFile == null )
            return;

        try
        {
            var json    = OpenIdConnectConfiguration.Write(Configuration);
            var tmpName = cacheFile + ".tmp";

            if ( File.Exists(tmpName) )
                File.Delete(tmpName);

            File.WriteAllText(tmpName, json);

            if ( File.Exists(cacheFile) )
                File.Delete(cacheFile);

            File.Move(tmpName, cacheFile);

            _log.LogDebug($"Oidc configuration cached into {cacheFile}");
        }
        catch (Exception ex)
        {
            _log.LogWarning($"Oidc configuration can't be saved to {cacheFile}: {ex.Message}");
        }
    }

    private static string? GetCacheFileName(SecuritySettings settings)
    {
        var cacheFile = settings.Oidc.ConfigCacheFile;
        if (cacheFile == null)
            return cacheFile;

        cacheFile = Environment.ExpandEnvironmentVariables(cacheFile);

        if (cacheFile.Contains('%'))
            cacheFile = Path.Combine(Path.GetTempPath(), ".oidc.settings.json");
        
        settings.Oidc.ConfigCacheFile = cacheFile;

        return cacheFile;
    }
}