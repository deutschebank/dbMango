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
/// Represents the parameters required for database configuration.
/// </summary>
public class DatabaseParams
{
    /// <summary>
    /// Gets or sets the contact information for the database.
    /// </summary>
    public string Contacts { get; set; } = "";

    /// <summary>
    /// Gets or sets the MongoDB connection URL.
    /// </summary>
    public string MongoDbUrl { get; set; } = "";

    /// <summary>
    /// Gets or sets the MongoDB database name.
    /// </summary>
    public string MongoDbDatabase { get; set; } = "";

    /// <summary>
    /// Gets or sets the username for user authentication.
    /// </summary>
    public string UserAuthUser { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the password for user authentication.
    /// </summary>
    public string UserAuthPassword { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the authentication database for user authentication.
    /// </summary>
    public string? UserAuthAuthDatabase { get; set; }

    /// <summary>
    /// Gets or sets the authentication method for user authentication.
    /// </summary>
    public string? UserAuthMethod { get; set; }

    /// <summary>
    /// Gets or sets the username for admin authentication.
    /// </summary>
    public string AdminAuthUser { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the password for admin authentication.
    /// </summary>
    public string AdminAuthPassword { get; set; } = "admin";

    /// <summary>
    /// Gets or sets the authentication database for admin authentication.
    /// </summary>
    public string? AdminAuthAuthDatabase { get; set; }

    /// <summary>
    /// Gets or sets the authentication method for admin authentication.
    /// </summary>
    public string? AdminAuthMethod { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use a direct connection to the database.
    /// </summary>
    public bool DirectConnection { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use TLS for the connection.
    /// </summary>
    public bool UseTls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether shard access is allowed.
    /// </summary>
    public bool AllowShardAccess { get; set; }
}
