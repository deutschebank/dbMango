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
﻿namespace Rms.Risk.Mango.Interfaces;

/// <summary>
/// Represents the settings required to establish a connection to an Oracle database.
/// </summary>
public class OracleConnectionSettings
{
    /// <summary>
    /// Gets or sets the connection string for the Oracle database.
    /// </summary>
    public string ConnectionString { get; set; } = "";

    /// <summary>
    /// Gets or sets the password for the Oracle database connection.
    /// </summary>
    public string Password { get; set; } = "";

    /// <summary>
    /// Gets or sets the connection string for the audit database, if applicable.
    /// </summary>
    public string? AuditConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the password for the audit database connection, if applicable.
    /// </summary>
    public string? AuditPassword { get; set; }

    /// <summary>
    /// Gets or sets the path to the Oracle wallet, if applicable.
    /// </summary>
    public string? Wallet { get; set; }

    /// <summary>
    /// Gets or sets the TNS_ADMIN directory path, if applicable.
    /// </summary>
    public string? TnsAdmin { get; set; }
}