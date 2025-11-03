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
﻿using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;

namespace Rms.Service.Bootstrap.Logging;

/// <summary>
/// Extension methods for Splunk support
/// </summary>
public static class SplunkLoggerExtensions
{
    /// <summary>
    /// Add direct Splunk forwarder logger
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddSplunkLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, SplunkLoggerProvider>());
        LoggerProviderOptions.RegisterProviderOptions<SplunkLoggerConfiguration, SplunkLoggerProvider>(builder.Services);

        return builder;
    }

    /// <summary>
    /// Add direct Splunk forwarder logger with specified configuration
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static ILoggingBuilder AddSplunkLogger(
        this ILoggingBuilder              builder,
        Action<SplunkLoggerConfiguration> configure
    )
    {
        builder.AddSplunkLogger();
        builder.Services.Configure(configure);

        return builder;
    }
}