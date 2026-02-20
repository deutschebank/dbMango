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
using MongoDB.Driver;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Pivot.UI.Services;

namespace Rms.Risk.Mango.Services.Audit;

// ReSharper disable InconsistentNaming
public class AuditService(MongoDbConfigRecord _config, MongoDbSettings _settings, int _auditExpireDays, ILogger<AuditService> _log, string? _databaseInstance = null) : IAuditService
// ReSharper restore InconsistentNaming
{
    public const string AuditCollection = DatabaseStructureLoader.AuditCollection;

    private readonly IMongoDatabase _database = MongoDbHelper.GetDatabase(_config, _settings, _databaseInstance);

    public void PreCheck(BsonDocument command)
    {
        if (command.ToJson().Contains(AuditCollection))
            throw new ApplicationException("Forbidden command");
    }

    public async Task Record(AuditRecord rec, CancellationToken token = default)
    {
        if ( _config.DisableDbMangoCollections)
            return;

        var commandType = rec.Command.ElementAt(0).Name ?? "";
        if (MongoDbCommandHelper.IsReadOnlyCommand(commandType))
            return;

        string? shortError = null;
        if (rec.Error != null)
        {
            var pos = rec.Error.IndexOf('\n');
            if (pos >= 0)
            {
                shortError = rec.Error![..pos].Replace("\r", "");
                shortError = shortError[..Math.Min(shortError.Length, 128)];
            }
        }

        var doc = new BsonDocument(new Dictionary<string, object?>()
        {
            ["_id"]         = Guid.NewGuid().ToString("N"),
            ["expire-at"]   = DateTime.UtcNow + TimeSpan.FromDays(_auditExpireDays),
            ["ts"]          = rec.Timestamp,
            ["collection"]  = rec.Command.ElementAt(0).Value,
            ["database"]    = rec.DatabaseName,
            ["email"]       = rec.Email,
            ["ticket"]      = rec.Ticket,
            ["ok"]          = rec.Success,
            ["error"]       = shortError,
            ["commandType"] = commandType,
            ["command"]     = rec.Command
        });

        _log.LogInformation(doc.ToJson());

        await _database.GetCollection<BsonDocument>(AuditCollection).InsertOneAsync(doc, new (), token);
    }

    public async Task<List<AuditRecord>> Audit(DateTime startDate, DateTime endDate, CancellationToken token = default)
    {
        if ( _config.DisableDbMangoCollections)
            return [];


        var filter = $@"{{
    ""$and"" : [
        {{ ts : {{ ""$gte"" : ISODate(""{startDate:yyyy-MM-dd}T00:00:00"") }} }},
        {{ ts : {{ ""$lte"" : ISODate(""{endDate:yyyy-MM-dd}T23:59:59"") }} }}
    ]
}}";

        var findOptions = new FindOptions<BsonDocument, BsonDocument>()
        {
            BatchSize  = 1000
        };

        var coll = _database.GetCollection<BsonDocument>(AuditCollection);

        var cursor = await coll.FindAsync(filter, findOptions, token);

        var list = new List<AuditRecord>();

        while (await cursor.MoveNextAsync(token))
        {
            foreach (var doc in cursor.Current)
            {
                if (doc.IsBsonNull)
                    continue;

                var rec = new AuditRecord
                (
                    DatabaseName: GetStr(doc,"database"),
                    Timestamp   : doc.GetValue("ts"      , DateTime.MinValue).AsBsonDateTime.AsUniversalTime,
                    Email       : GetStr(doc,"email" ),
                    Ticket      : GetStr(doc,"ticket"),
                    Success     : doc.GetValue("ok"      , false).ToBoolean(),
                    Error       : GetStr(doc,"error" ),
                    Command     : BsonDocument.Parse(doc.GetValue("command", "").ToString() ?? "")
                );

                list.Add(rec);
            }
        }

        return list;
    }

    private static string GetStr(BsonDocument doc, string name)
    {
        if (!doc.Contains(name) || doc[name].IsBsonNull )
            return "";
        return doc[name].ToString() ?? "";
    }

}