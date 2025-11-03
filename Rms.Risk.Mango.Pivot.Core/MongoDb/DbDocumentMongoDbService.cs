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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public delegate IMongoDbService<T> DbDocumentMongoDbServiceFactory<T>(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? database = null, string? url = null, string? databaseInstance = null) where T : IMongoDbDocumentBase;


/// <summary>
/// An implementation of the mongo service interface for collections of documents that service from IMongoDbDocumentBase
/// </summary>
/// <typeparam name="T"></typeparam>
[ExcludeFromCodeCoverage]
public class DbDocumentMongoDbService<T> : MongoDbServiceBase<T> where T: class, IMongoDbDocumentBase
{
    /// <summary>
    /// An implementation of the mongo service interface for collections of documents that service from IMongoDbDocumentBase
    /// </summary>
    public DbDocumentMongoDbService(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? databaseInstance = null) 
        : base(config, settings, collectionName, databaseInstance)
    {
    }

    public static IMongoDbService<T> DefaultFactory(MongoDbConfigRecord config, MongoDbSettings settings, string collectionName, string? database = null, string? url = null, string? databaseInstance = null) 
        => new DbDocumentMongoDbService<T>(config, settings, collectionName, databaseInstance);

    protected override int ProcessDuplicates(HashSet<string> duplicateIds, List<T> dataList)
    {
        //upset the duplicate bson items
        var replaced = 0;

        //extract all the items in the datalist with the duplicate ids
        foreach (var item in dataList.Where(x => duplicateIds.Contains(x.Id)))
        {
            Collection.ReplaceOne(x => x.Id == item.Id, item); //TODO figure out how to call the base impl
            replaced += 1;
        }

        return replaced;
    }
}

public static class DbDocumentMongoDbServiceExtensions
{
    public static IServiceCollection AddMongoDbService<T>(this IServiceCollection services) where T : class, IMongoDbDocumentBase
    {
        services.AddSingleton<DbDocumentMongoDbServiceFactory<T>>(DbDocumentMongoDbService<T>.DefaultFactory);
        return services;
    }
}