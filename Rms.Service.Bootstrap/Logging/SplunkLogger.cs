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
﻿using System.Diagnostics;
using System.Reflection;

namespace Rms.Service.Bootstrap.Logging;

internal sealed class SplunkLogger(
    string                          name,
    SplunkMessageSender             sender,
    Func<SplunkLoggerConfiguration> getOptions)
    : ILogger
{
    public IDisposable BeginScope<TState>(TState state) where TState : notnull => null!;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= getOptions().MinLogLevel;

    public void Log<TState>(
        LogLevel                         logLevel,
        EventId                          eventId,
        TState                           state,
        Exception?                       exception,
        Func<TState, Exception?, string> formatter)
    {
//        var options = _getOptions();
        var extraInfo = $"  a={Assembly.GetExecutingAssembly().GetName().Name} u={Environment.UserName} p={Process.GetCurrentProcess().Id.ToString()} t=\"{Thread.CurrentThread.Name}\"";
        sender.Log($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss,fff} {LevelToString(logLevel),-5} {name,-32} {formatter(state, exception)}\n{extraInfo}\n");
    }

    private string LevelToString(LogLevel logLevel) =>
        logLevel switch
        {
            LogLevel.Trace       => "TRACE",
            LogLevel.Debug       => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning     => "WARN",
            LogLevel.Error       => "ERROR",
            LogLevel.Critical    => "FATAL",
            _                    => "DEBUG"
        };
}