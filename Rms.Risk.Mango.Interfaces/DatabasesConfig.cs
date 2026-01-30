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
﻿using Rms.Risk.Mango.Pivot.Core.MongoDb;

namespace Rms.Risk.Mango.Interfaces;

/// <summary>
/// Represents the configuration for multiple databases.
/// </summary>
public class DatabasesConfig
{
    /// <summary>
    /// Represents the configuration for a single database.
    /// </summary>
    public class DatabaseConfig
    {
        /// <summary>
        /// Represents LDAP group configurations for a database.
        /// </summary>
        public class LdapGroups
        {
            /// <summary>
            /// Gets or sets the LDAP group for administrators.
            /// </summary>
            public string Admin { get; set; } = "";

            /// <summary>
            /// Gets or sets the LDAP group for read-only access.
            /// </summary>
            public string ReadOnly { get; set; } = "";

            /// <summary>
            /// Gets or sets the LDAP group for read-write access.
            /// </summary>
            public string ReadWrite { get; set; } = "";

            /// <summary>
            /// Creates a deep copy of the current <see cref="LdapGroups"/> instance.
            /// </summary>
            /// <returns>A new <see cref="LdapGroups"/> instance with the same values.</returns>
            public LdapGroups Clone()
                => new()
                {
                    Admin = Admin,
                    ReadOnly = ReadOnly,
                    ReadWrite = ReadWrite
                };
        }

        /// <summary>
        /// Gets or sets the LDAP group configurations.
        /// </summary>
        public LdapGroups Groups { get; set; } = new();

        /// <summary>
        /// Gets or sets the MongoDB configuration record.
        /// </summary>
        public MongoDbConfigRecord Config { get; set; } = new();

        /// <summary>
        /// Gets or sets the contact information for the database.
        /// </summary>
        public string Contacts { get;               set; } = "";
        /// <summary>
        /// Gets or sets additional information. Can be used by the plugin to check task.
        /// </summary>
        public string Comments { get; set; } = "";

        /// <summary>
        /// Creates a deep copy of the current <see cref="DatabaseConfig"/> instance.
        /// </summary>
        /// <returns>A new <see cref="DatabaseConfig"/> instance with the same values.</returns>
        public DatabaseConfig Clone()
        {
            var c = new DatabaseConfig
            {
                Config = Config.Clone(),
                Groups = Groups.Clone(),
                Contacts = Contacts
            };
            return c;
        }
    }

    /// <summary>
    /// Gets or sets the dictionary of database configurations, keyed by database name.
    /// </summary>
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, DatabaseConfig> Databases { get; set; } = new();
}