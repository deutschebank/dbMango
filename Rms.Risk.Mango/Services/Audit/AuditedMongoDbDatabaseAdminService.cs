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
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pivot.Core.MongoDb;

namespace Rms.Risk.Mango.Services.Audit;

public class AuditedMongoDbDatabaseAdminService(
    MongoDbConfigRecord _config,
    MongoDbSettings     _settings,
    IUserSession        _session,
    IAuditService       _audit,
    string?             databaseInstance = null ) : IMongoDbDatabaseAdminService
{
    private        MongoDbDatabaseAdminService       _mongo = new (_config, _settings, databaseInstance ?? _session.DatabaseInstance);

    public string Database => _mongo.Database;

    public         Task<IReadOnlyCollection<string>> ListCollections(CancellationToken token = default) => _mongo.ListCollections(token);

    public async Task<BsonDocument> RunCommand(BsonDocument doc, string? originalCommand, CancellationToken token = default)
    {
        _audit.PreCheck(doc);

        if ( !MongoDbCommandHelper.IsReadOnlyCommand(doc) && !await _session.HasValidTask())
        {

            var origCommand = string.IsNullOrWhiteSpace(originalCommand) 
                ? doc 
                : BsonDocument.Parse(originalCommand)
                ;

            var message = $"Ticket check failed: {_session.TaskCheckError}";
            var rec = new AuditRecord(
                _session.Database,
                DateTime.UtcNow,
                _session.User.GetEmail(),
                _session.TaskNumber ?? "",
                false,
                origCommand,
                message
            );
            await _audit.Record(rec, token);
            throw new ApplicationException(message);
        }

        try
        {
            var ret = await _mongo.RunCommand(doc, originalCommand, token);

            AuditRecord rec = ret.TryGetValue("ok", out var value) && !value.ToBoolean()
                ? new(
                    _session.Database,
                    DateTime.UtcNow,
                    _session.User.GetEmail(),
                    _session.TaskNumber ?? "",
                    false,
                    doc,
                    ret["errmsg"]?.ToString()
                )
                : new (
                    _session.Database,
                    DateTime.UtcNow,
                    _session.User.GetEmail(),
                    _session.TaskNumber ?? "",
                    true,
                    doc
                );

            await _audit.Record(rec, token);
            return ret;
        }
        catch (Exception ex)
        {
            var rec = new AuditRecord(
                _session.Database,
                DateTime.UtcNow,
                _session.User.GetEmail(),
                _session.TaskNumber ?? "",
                false,
                doc,
                ex.ToString()
            );
            await _audit.Record(rec, token);
            throw;
        }
    }
}