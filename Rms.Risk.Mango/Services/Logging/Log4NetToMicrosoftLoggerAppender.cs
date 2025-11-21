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
using log4net.Appender;
using log4net.Core;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Rms.Risk.Mango.Services.Logging;

public class Log4NetToMicrosoftLoggerAppender(ILoggerFactory _loggerFactory) : AppenderSkeleton
{
    private readonly ILoggerFactory _loggerFactory = _loggerFactory ?? throw new ArgumentNullException(nameof(_loggerFactory));

    private readonly ConcurrentDictionary<string, ILogger> _loggerCache = new();

    protected override void Append(LoggingEvent loggingEvent)
    {
        if (loggingEvent.Level == null || loggingEvent.LoggerName == null)
            return;

        // Get logger by name from the cache or create a new one
        var logger = _loggerCache.GetOrAdd(loggingEvent.LoggerName, name => _loggerFactory.CreateLogger(name));

        var logLevel = MapLogLevel(loggingEvent.Level);
        var message = loggingEvent.RenderedMessage;

        if (message == null)
            return;

        if (loggingEvent.ExceptionObject != null)
        {
            logger.Log(logLevel, loggingEvent.ExceptionObject, message);
        }
        else
        {
            logger.Log(logLevel, message);
        }
    }

    private static LogLevel MapLogLevel(Level log4netLevel)
    {
        return  log4netLevel == Level.Debug ? LogLevel.Debug :
                log4netLevel == Level.Info  ? LogLevel.Information :
                log4netLevel == Level.Warn  ? LogLevel.Warning :
                log4netLevel == Level.Error ? LogLevel.Error :
                log4netLevel == Level.Fatal ? LogLevel.Critical :
                                              LogLevel.None;
    }
}