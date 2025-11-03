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
﻿using Microsoft.AspNetCore.Components;
using MongoDB.Bson;
using Rms.Risk.Mango.Pivot.Core.MongoDb;

namespace Rms.Risk.Mango.Services;

public record CommandDef(string Name, MarkupString Description, bool AdminOnly);

public interface ICommandListService
{
    Task<IReadOnlyCollection<CommandDef>> GetCommands(IMongoDbDatabaseAdminService admin, CancellationToken token);
}

internal class CommandListService : ICommandListService
{
    private static readonly List<CommandDef> _commands = [];
    private static readonly Lock _commandsLock = new();

    public async Task<IReadOnlyCollection<CommandDef>> GetCommands(IMongoDbDatabaseAdminService admin, CancellationToken token)
    {
        lock (_commandsLock)
        {
            if (_commands.Count > 0 )
                return _commands;
        }

        var listCommands = BsonDocument.Parse("{ listCommands: 1}");
        var doc       = await admin.RunCommand(listCommands, token);

        lock (_commands)
        {
            _commands.Clear();
            foreach (var cmd in doc["commands"]?.AsBsonDocument.Elements ?? [])
            {
                var name    = cmd.Name;
                var help    = cmd.Value["help"].AsString.Replace("\n", "<br/>");
                var isAdmin = cmd.Value["adminOnly"].AsBoolean;

                _commands.Add(new(name, new (help), isAdmin));
            }
            return _commands;
        }
    }
}