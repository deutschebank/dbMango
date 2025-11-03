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
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using System.Diagnostics.CodeAnalysis;


namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public delegate IMongoDbService<BsonDocument> BsonMongoDbServiceFactory(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? databaseInstance = null);

/// <summary>
/// An implementation of the mongoservice interface that allows you to interact with a collection of BsonDocuments
/// This is ultimately all our collections, so this implementation can be used as a generic interaface to all our collections
/// </summary>
[ExcludeFromCodeCoverage]
public class BsonMongoDbService : MongoDbServiceBase<BsonDocument>
{
    /// <summary>
    /// An implementation of the mongoservice interface that allows you to interact with a collection of BsonDocuments
    /// This is ultimately all our collections, so this implementation can be used as a generic interaface to all our collections
    /// </summary>
    public BsonMongoDbService(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? databaseInstance = null)
        : base(config, settings, collectionName, databaseInstance)
    {
    }

    public static IMongoDbService<BsonDocument> DefaultFactory(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? databaseInstance = null) 
        => new BsonMongoDbService(config, settings, collectionName, databaseInstance);

    protected override int ProcessDuplicates(HashSet<string> duplicateIds, List<BsonDocument> dataList)
    {
        //upsert the duplicate bson items
        var replaced = 0;

        foreach (var item in dataList.Select( x => new { Id = x.GetElement("_id").Value.AsString, Data = x }).Where(x => duplicateIds.Contains(x.Id)))
        {
            Collection.ReplaceOne($"{{ _id : \"{item.Id}\"}}", item.Data);
            replaced += 1;
        }

        return replaced;
    }
}

public static class BsonMongoDbServiceExtensions
{
    public static IServiceCollection AddBsonMongoDbServiceFactory(this IServiceCollection services)
    {
        services.AddSingleton<BsonMongoDbServiceFactory>(BsonMongoDbService.DefaultFactory);
        return services;
    }
}