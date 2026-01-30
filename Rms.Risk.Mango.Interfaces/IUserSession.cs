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
﻿using MongoDB.Bson;
using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Microsoft.AspNetCore.Authorization;
using Rms.Service.Bootstrap.Security;

namespace Rms.Risk.Mango.Interfaces;

/// <summary>
/// Interface representing a user session, providing access to user-related services, database configurations, 
/// and MongoDB services.
/// </summary>
public interface IUserSession
{
    /// <summary>
    /// Gets the user service for the current session.
    /// </summary>
    UserService User { get; }

    /// <summary>
    /// Gets or sets the task number associated with the session.
    /// </summary>
    string? TaskNumber { get; set; }

    /// <summary>
    /// Gets the error message, if any, related to task checks.
    /// </summary>
    string? TaskCheckError { get; }

    /// <summary>
    /// Gets or sets the name of the database being used in the session.
    /// </summary>
    string Database { get; set; }

    /// <summary>
    /// Gets a value indicating whether database instance selection is allowed.
    /// </summary>
    bool IsDatabaseInstanceSelectionAllowed { get; }

    /// <summary>
    /// Gets or sets the name of the collection being used in the session.
    /// </summary>
    string Collection { get; set; }

    /// <summary>
    /// Gets or sets the database instance being used in the session.
    /// </summary>
    string DatabaseInstance { get; set; }

    /// <summary>
    /// Gets the MongoDB service for interacting with the database.
    /// </summary>
    IMongoDbService<BsonDocument> MongoDb { get; }

    /// <summary>
    /// Gets the MongoDB admin service for managing the database.
    /// </summary>
    IMongoDbDatabaseAdminService MongoDbAdmin { get; }

    /// <summary>
    /// Gets the MongoDB admin service for managing the admin database.
    /// </summary>
    IMongoDbDatabaseAdminService MongoDbAdminForAdminDatabase { get; }

    /// <summary>
    /// Gets the pivot table data source for the session.
    /// </summary>
    IPivotTableDataSource PivotDataSource { get; }

    /// <summary>
    /// Gets the audit service for logging and tracking changes.
    /// </summary>
    IAuditService Audit { get; }

    /// <summary>
    /// Gets the configuration record for the database.
    /// </summary>
    MongoDbConfigRecord DatabaseConfig { get; }

    /// <summary>
    /// Gets the LDAP groups configuration for the session.
    /// </summary>
    DatabasesConfig.DatabaseConfig.LdapGroups LdapGroups { get; }

    /// <summary>
    /// Checks if the session has a valid task.
    /// </summary>
    /// <returns>A task that resolves to true if the task is valid; otherwise, false.</returns>
    Task<bool> HasValidTask(bool checkExtraInfo = true);

    /// <summary>
    /// Determines if the user can access a specific resource based on the provided policy and database name.
    /// </summary>
    /// <param name="auth">The authorization service.</param>
    /// <param name="policyName">The name of the policy to check.</param>
    /// <param name="databaseName">The name of the database to check access for.</param>
    /// <returns>A task that resolves to true if access is allowed; otherwise, false.</returns>
    Task<bool> CanAccess(IAuthorizationService auth, string policyName, string databaseName);

    /// <summary>
    /// Determines if instance selection is allowed for the specified database.
    /// </summary>
    /// <param name="database">The name of the database.</param>
    /// <returns>True if instance selection is allowed; otherwise, false.</returns>
    bool IsInstanceSelectionAllowed(string database);

    /// <summary>
    /// Event triggered when the database changes.
    /// </summary>
    event Action? DatabaseChanged;

    /// <summary>
    /// Gets a custom MongoDB admin service for the specified database and instance.
    /// </summary>
    /// <param name="databaseName">The name of the database.</param>
    /// <param name="databaseInstance">The name of the database instance.</param>
    /// <returns>The custom MongoDB admin service.</returns>
    IMongoDbDatabaseAdminService GetCustomAdmin(string databaseName, string databaseInstance);

    /// <summary>
    /// Gets a shard connection for the specified host and port.
    /// </summary>
    /// <param name="host">The host of the shard.</param>
    /// <param name="port">The port of the shard.</param>
    /// <returns>The MongoDB admin service for the shard connection.</returns>
    IMongoDbDatabaseAdminService GetShardConnection(string host, int port);
}