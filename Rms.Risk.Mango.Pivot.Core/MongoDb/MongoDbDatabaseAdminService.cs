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
using MongoDB.Bson;
using MongoDB.Driver;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public class MongoDbDatabaseAdminService(MongoDbConfigRecord _config, MongoDbSettings _settings, string? _databaseInstance = null) : IMongoDbDatabaseAdminService
{
    private readonly Lazy<IMongoDatabase> _db = new(() => MongoDbHelper.GetDatabase(_config, _settings, _databaseInstance ?? _config.MongoDbDatabase));

    public string Database => !string.IsNullOrWhiteSpace(_databaseInstance) 
        ? _databaseInstance 
        : _config.MongoDbDatabase
        ;

    public async Task<IReadOnlyCollection<string>> ListCollections(CancellationToken token = default)
    {
        var db     = MongoDbHelper.GetDatabase(_config, _settings, Database);
        var cursor = await db.ListCollectionNamesAsync(cancellationToken: token);
        var docs   = await cursor.ToListAsync(cancellationToken: token);
        var coll   = docs
           .Where(x => !string.IsNullOrWhiteSpace(x))
           .Where(x => x != null)
           .Distinct(StringComparer.OrdinalIgnoreCase)
           .OrderBy(x => x)
           .ToList();

        return coll;
    }

    public async Task<BsonDocument> RunCommand(BsonDocument doc, string? originalCommand, CancellationToken token = default )
    {
        if ( doc.ElementCount == 0 )
            throw new ApplicationException("Empty command");

        var res = await _db.Value.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(doc), cancellationToken: token);

        if (res.Contains("ok") && !res["ok"].ToBoolean())
            throw new ApplicationException($"Failed to execute command: {doc.ElementAt(0).Name}");

        return res;
    }

}
