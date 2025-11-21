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
/// Represents the base context for database configurations.
/// </summary>
public class ContextBase
{
    /// <summary>
    /// Gets or sets the type of the context.
    /// </summary>
    public string Type { get; set; } = "dbMangoDatabase";

    /// <summary>
    /// Gets or sets the name of the context.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Gets or sets the unique identifier of the context.
    /// </summary>
    public long ID { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the parent context.
    /// </summary>
    public long ParentID { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context is a template.
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the context is proposed for deletion.
    /// </summary>
    public bool ProposedForDeletion { get; set; }
}

/// <summary>
/// Represents the database configuration context for a Mango database.
/// </summary>
public class DbMangoDatabaseConfigContext : ContextBase
{
    /// <summary>
    /// Gets or sets the database parameters for the configuration.
    /// </summary>
    public DatabaseParams DatabaseParams { get; set; } = new();

    /// <summary>
    /// Gets or sets the LDAP group parameters for the configuration.
    /// </summary>
    public DatabasesConfig.DatabaseConfig.LdapGroups LdapParams { get; set; } = new();
}


/// <summary>
/// Interface for managing database configuration storage operations.
/// </summary>
public interface IDatabaseConfigurationStorage
{
    /// <summary>
    /// Reads a database configuration by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the database configuration.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the database configuration context.</returns>
    Task<DbMangoDatabaseConfigContext> Read(long id, CancellationToken token);

    /// <summary>
    /// Creates a new database configuration.
    /// </summary>
    /// <param name="ctx">The database configuration context to create.</param>
    /// <param name="email">The email of the user performing the operation.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the unique identifier of the created configuration.</returns>
    Task<long> Create(DbMangoDatabaseConfigContext ctx, string email, CancellationToken token);

    /// <summary>
    /// Updates an existing database configuration.
    /// </summary>
    /// <param name="ctx">The database configuration context to update.</param>
    /// <param name="email">The email of the user performing the operation.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Update(DbMangoDatabaseConfigContext ctx, string email, CancellationToken token);

    /// <summary>
    /// Deletes a database configuration by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the database configuration to delete.</param>
    /// <param name="email">The email of the user performing the operation.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Delete(long id, string email, CancellationToken token);

    /// <summary>
    /// Lists all database configurations associated with a specific user.
    /// </summary>
    /// <param name="email">The email of the user whose configurations are to be listed.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of database configuration contexts.</returns>
    Task<List<DbMangoDatabaseConfigContext>> List(string email, CancellationToken token);

    /// <summary>
    /// Retrieves configuration settings for a specific user.
    /// </summary>
    /// <param name="email">The email of the user whose configuration settings are to be retrieved.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a dictionary of configuration settings.</returns>
    Task<Dictionary<string, string>> GetConfiguration(string email, CancellationToken token);

    /// <summary>
    /// Updates configuration settings for a specific user.
    /// </summary>
    /// <param name="configuration">The dictionary of configuration settings to update.</param>
    /// <param name="email">The email of the user performing the operation.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task UpdateConfiguration(Dictionary<string, string> configuration, string email, CancellationToken token);
}