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
﻿using Microsoft.Extensions.Hosting;

namespace Rms.Risk.Mango.Interfaces;

/// <summary>
/// Defines the contract for a plugin that integrates with dbMango.
/// </summary>
public interface IDbMangoPlugin
{
    /// <summary>
    /// Configures the services required by the plugin.
    /// </summary>
    /// <param name="builder">The application builder used to configure services.</param>
    /// <returns>The updated application builder.</returns>
    IHostApplicationBuilder ConfigureServices(IHostApplicationBuilder builder);

    /// <summary>
    /// Creates a secure audit service using the provided Oracle connection settings.
    /// </summary>
    /// <param name="settings">The Oracle connection settings used to configure the audit service.</param>
    /// <returns>An instance of <see cref="IAuditService"/> if successful; otherwise, <c>null</c>.</returns>
    IAuditService? CreateSecureAuditService(OracleConnectionSettings settings);
}