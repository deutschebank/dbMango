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
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public static class MongoDbCachingHelper
{
    public static async Task<string[]> LoadCachedCobDatesAsync(this MongoDbDataSource source, string collectionName, CancellationToken token = default)
    {
        var coll   = source.GetCollectionWithRetries<BsonDocument>(collectionName+"-Meta");
        var cursor = await coll.FindAsync("{ _id : \"CachedCobDates\"}", cancellationToken: token);

        while ( await cursor.MoveNextAsync(token) )
        {
            var batch = cursor.Current;
            foreach ( var doc in batch )
            {
                if ( doc.IsBsonNull )
                    return [];

                var res = doc.ToDictionary();

                return res["CobDates"] is not object[] cobs 
                        ? [] 
                        : cobs.Select( x => x as string )
                              .Where( x => !string.IsNullOrWhiteSpace( x ) )
                              .OfType<string>()
                              .ToArray()
                    ;
            }
        }
        return [];
    }

    private class CachedDepartmentsDoc
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [BsonId] public string   Id          { get; set; } = "";
        public          string[] Departments { get; set; } = [];
        public          DateTime CachedOnUtc { get; set; }
    }

    private const string CachedDepartmentsDocName = "CachedDepartments";

    public static async Task<Tuple<bool,string[]>> LoadCachedDepartmentsAsync(this MongoDbDataSource source, string collectionName, CancellationToken token = default)
    {
        var coll   = source.GetCollectionWithRetries<CachedDepartmentsDoc>(collectionName+"-Meta");
        var cursor = await coll.FindAsync($"{{ _id : \"{CachedDepartmentsDocName}\"}}", cancellationToken: token);

        while ( await cursor.MoveNextAsync(token) )
        {
            var batch = cursor.Current;
            foreach ( var doc in batch )
            {
                var expireAt     = doc.CachedOnUtc + TimeSpan.FromHours(1);
                var isStillValid = expireAt > DateTime.UtcNow;

                return Tuple.Create(
                    isStillValid, 
                    doc.Departments.Where( x => !string.IsNullOrWhiteSpace( x ) ).ToArray() 
                );
            }
        }
        return Tuple.Create(false, Array.Empty<string>());
    }

    public static async Task CacheDepartments(this MongoDbDataSource source, string collectionName, string [] departments, CancellationToken token = default)
    {
        if ( departments.Length == 0 )
            return;

        var doc = new CachedDepartmentsDoc()
        {
            Id          = CachedDepartmentsDocName,
            CachedOnUtc = DateTime.UtcNow,
            Departments = departments
        };

        var coll = source.GetCollectionWithRetries<CachedDepartmentsDoc>(collectionName + "-Meta");
        await coll.ReplaceOneAsync( $"{{ _id : \"{CachedDepartmentsDocName}\"}}", doc, new ReplaceOptions {IsUpsert = true}, token);
    }

    private class CachedDesksDoc
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [BsonId] public string             Id                { get; set; } = "";
        public          (string, string)[] DeskAndDepartment { get; set; } = [];
        public          DateTime           CachedOnUtc       { get; set; }
    }

    private const string CachedDesksDocName = "CachedDesks";

    public static async Task<Tuple<bool,(string, string)[]>> LoadCachedDesksAsync(this MongoDbDataSource source, string collectionName, CancellationToken token = default)
    {
        var coll   = source.GetCollectionWithRetries<CachedDesksDoc>(collectionName+"-Meta");
        var cursor = await coll.FindAsync($"{{ _id : \"{CachedDesksDocName}\"}}", cancellationToken: token);

        while ( await cursor.MoveNextAsync(token) )
        {
            var batch = cursor.Current;
            foreach ( var doc in batch )
            {
                var expireAt     = doc.CachedOnUtc + TimeSpan.FromHours(24);
                var isStillValid = expireAt > DateTime.UtcNow;

                return Tuple.Create(
                    isStillValid, 
                    doc.DeskAndDepartment.Where( x => !string.IsNullOrWhiteSpace( x.Item1 ) &&
                                                     !string.IsNullOrWhiteSpace( x.Item2 )).ToArray() 
                );
            }
        }
        return Tuple.Create(false, Array.Empty<(string, string)>());
    }

    public static async Task CacheDesks(this MongoDbDataSource source, string collectionName, (string, string) [] desks, CancellationToken token = default)
    {
        if ( desks.Length == 0 )
            return;

        var doc = new CachedDesksDoc()
        {
            Id                = CachedDesksDocName,
            CachedOnUtc       = DateTime.UtcNow,
            DeskAndDepartment = desks
        };

        var coll = source.GetCollectionWithRetries<CachedDesksDoc>(collectionName + "-Meta");
        await coll.ReplaceOneAsync( $"{{ _id : \"{CachedDesksDocName}\"}}", doc, new ReplaceOptions {IsUpsert = true}, token);
    }

    public static async Task CacheCobDates(this MongoDbDataSource source, string collectionName, string [] cobs, CancellationToken token = default)
    {
        if ( cobs.Length == 0 )
            return;

        var doc  = new BsonDocument(new Dictionary<string, string[]> { ["CobDates"] = cobs});
        var coll = source.GetCollectionWithRetries<BsonDocument>(collectionName + "-Meta");
        await coll.ReplaceOneAsync( "{_id : \"CachedCobDates\"}", doc, new ReplaceOptions {IsUpsert = true}, token);
    }

}
