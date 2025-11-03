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
﻿using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Microsoft.Extensions.Options;

namespace Rms.Service.Bootstrap.Logging;

/// <summary>
/// Direct Splunk connection logger provider
/// </summary>
[UnsupportedOSPlatform("browser")]
[ProviderAlias("Splunk")]
public sealed class SplunkLoggerProvider : ILoggerProvider
{
    private readonly SplunkMessageSender                        _sender;
    private readonly IDisposable?                               _onChangeToken;
    private          SplunkLoggerConfiguration                  _currentConfig;
    private readonly ConcurrentDictionary<string, SplunkLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="config"></param>
    public SplunkLoggerProvider(IOptionsMonitor<SplunkLoggerConfiguration> config)
    {
        _currentConfig = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);
        _sender        = new(GetCurrentConfig, null);
    }

    /// <summary>
    /// Create splunk logger
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public ILogger CreateLogger(string categoryName) =>
        _loggers.GetOrAdd(categoryName, name => new(name, _sender, GetCurrentConfig));

    private SplunkLoggerConfiguration GetCurrentConfig() => _currentConfig;

    /// <summary>
    /// Dispose all created loggers
    /// </summary>
    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}