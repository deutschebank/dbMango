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

namespace Rms.Risk.Mango.Interfaces;

/// <summary>
/// Represents an audit record containing details about a database operation.
/// </summary>
/// <param name="DatabaseName">The name of the database where the operation occurred.</param>
/// <param name="Timestamp">The timestamp of when the operation was performed.</param>
/// <param name="Email">The email of the user who performed the operation.</param>
/// <param name="Ticket">The ticket identifier associated with the operation.</param>
/// <param name="Success">Indicates whether the operation was successful.</param>
/// <param name="Command">The MongoDB command executed during the operation.</param>
/// <param name="Error">Optional error message if the operation failed.</param>
public record AuditRecord(
    string DatabaseName, 
    DateTime Timestamp, 
    string Email, 
    string Ticket, 
    bool Success, 
    BsonDocument Command, 
    string? Error = null);

/// <summary>
/// Defines methods for auditing database operations.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Performs a pre-check on the provided MongoDB command.
    /// </summary>
    /// <param name="command">The MongoDB command to validate before execution.</param>
    void PreCheck(BsonDocument command);

    /// <summary>
    /// Records an audit entry for a database operation.
    /// </summary>
    /// <param name="rec">The audit record containing details of the operation.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task Record(AuditRecord rec, CancellationToken token = default);

    /// <summary>
    /// Retrieves a list of audit records within a specified date range.
    /// </summary>
    /// <param name="startDate">The start date of the range to retrieve audit records.</param>
    /// <param name="endDate">The end date of the range to retrieve audit records.</param>
    /// <param name="token">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation, containing a list of audit records.</returns>
    Task<List<AuditRecord>> Audit(DateTime startDate, DateTime endDate, CancellationToken token = default);
}