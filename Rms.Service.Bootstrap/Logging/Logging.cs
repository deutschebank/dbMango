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
﻿namespace Rms.Service.Bootstrap.Logging;

/// <summary>
/// Shared logger. Please, use in static classes only.
/// For normal classes use dependency injection to inject ILogger into a constructor.
/// </summary>
public static class Logging
{
    /// <summary>
    /// Factory to create loggers for static classes
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; set; } = new LoggerFactory();

    /// <summary>
    /// Create logger for a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ILogger<T> GetLogger<T>()              => LoggerFactory.CreateLogger<T>();        
    /// <summary>
    /// Create logger for a category
    /// </summary>
    /// <param name="categoryName"></param>
    /// <returns></returns>
    public static ILogger GetLogger(string categoryName) => LoggerFactory.CreateLogger(categoryName);
    /// <summary>
    /// Create logger for a type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static ILogger GetLogger(Type   type)         => LoggerFactory.CreateLogger(type);
}
