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
/// Provides helper methods for MongoDB command operations.
/// </summary>
public static class MongoDbCommandHelper
{
    /// <summary>
    /// A set of MongoDB command names that are considered read-only and do not require auditing.
    /// </summary>
    private static readonly HashSet<string> _noAudit = new(StringComparer.InvariantCultureIgnoreCase)
    {
        "find",
        "aggregate",
        "listcollections",
        "listDatabases",
        "hello",
        "listIndexes",
        "collStats",
        "listCommands",
        "ping",
        "listShards",
        "getShardMap",
        "serverStatus",
        "balancerStatus",
        "dbStats",
        "buildInfo",
        "getShardVersion",
        "getParameter",
        "getLog",
        "rolesInfo",
        "usersInfo",
        "availableQueryOptions",
        "analyzeShardKey",
        "analyze",
        "currentOp",
        "connectionStatus",
        "replSetGetStatus",
    };

    /// <summary>
    /// Determines whether the specified MongoDB command is read-only.
    /// </summary>
    /// <param name="command">The MongoDB command represented as a <see cref="BsonDocument"/>.</param>
    /// <returns>
    /// <c>true</c> if the command is read-only; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsReadOnlyCommand(BsonDocument command)
    {
        if (command == null || command.IsBsonNull)
            return false;
        var commandType = command.ElementAt(0).Name;
        return IsReadOnlyCommand(commandType);
    }

    /// <summary>
    /// Determines whether the specified MongoDB command name is read-only.
    /// </summary>
    /// <param name="command">The name of the MongoDB command.</param>
    /// <returns>
    /// <c>true</c> if the command name is read-only; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsReadOnlyCommand(string? command)
    {
        if (!string.IsNullOrWhiteSpace(command) && _noAudit.Contains(command)) return true;
        return false;
    }
}