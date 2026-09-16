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

 using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Rms.Service.Bootstrap.Security;

public static class ServiceCollectionExtensions
{

    public static IServiceCollection ConfigureProtected<TOptions>(this IServiceCollection services, IConfigurationSection section)
        where TOptions : class, new() =>
        services.AddSingleton(provider =>
        {
            var logger          = provider.GetRequiredService<ILogger<ProtectedConfigurationSection>>();
            var passwordManager = provider.GetRequiredService<IPasswordManager>();
            var configuration   = provider.GetRequiredService<IConfiguration>();

            section = new ProtectedConfigurationSection(passwordManager, configuration, section, logger);

            var options = section.Get<TOptions>() ?? new();
            return Options.Create(options);
        });

    private class ProtectedConfigurationSection(
        IPasswordManager                       passwordManager,
        IConfiguration                         config,
        IConfigurationSection                  section,
        ILogger<ProtectedConfigurationSection> logger)
        : IConfigurationSection
    {
        public IConfigurationSection GetSection(string key) => new ProtectedConfigurationSection(passwordManager, config, section.GetSection(key), logger);

        public IEnumerable<IConfigurationSection> GetChildren() =>
            section.GetChildren()
                    .Select(x => new ProtectedConfigurationSection(passwordManager, config, x, logger));

        public IChangeToken GetReloadToken() => section.GetReloadToken();

        public string? this[string key]
        {
            get => GetProtectedValue(section[key], key);
            set => section[key] = value;
        }

        public string Key => section.Key;
        public string Path => section.Path;

        public string? Value
        {
            get => GetProtectedValue(section.Value, Key);
            set => section.Value = value;
        }

        private static readonly ConcurrentDictionary<string, bool> _reported = new();

        private string? GetProtectedValue(string? value, string key)
        {
            if (value == null)
                return null;

            // redirect first as parameter value may still be encrypted
            while (value.StartsWith("config:"))
            {
                var paramName = value["config:".Length..];
                if (config[paramName] is { } configValue)
                {
                    //logger.LogDebug($"Redirecting configuration item: Key=\"{key}\" Value=\"{configValue}\"");
                    value = configValue;
                }
                else
                {
                    if ( !_reported.ContainsKey(key))
                    {
                        logger.LogWarning($"Configuration item is not found: Key=\"{key}\" ParamName=\"{paramName}\"");
                        _reported[key] = true;
                    }
                    break;
                }
            }
            if (value.StartsWith('*') || value.StartsWith('@') || value.StartsWith('#'))
            {
                if ( !_reported.ContainsKey(key))
                {
                    logger.LogInformation($"Decrypting configuration item: Key=\"{key}\" Length={value.Length}");
                    _reported[key] = true;
                }
                return passwordManager.DecryptPassword(value);
            }

            return value;
        }
    }
}