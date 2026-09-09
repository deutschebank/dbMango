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
﻿using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public static class MongoDbCachingHelper
{
    private class CachedValuesDoc
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        [BsonId] public string   Id          { get; set; } = "";
        public          string[] Values      { get; set; } = [];
        public          DateTime CachedOnUtc { get; set; }
        public          DateTime ExpireAtUtc { get; set; }
    }

    private static string GetCachedDocId(string fieldName) => $"Cached{fieldName}";

    public static async Task<Tuple<bool, string[]>> LoadCached(this MongoDbDataSource source, string collectionName, string fieldName, TimeSpan ttl, CancellationToken token = default)
    {
        if ( string.IsNullOrWhiteSpace(fieldName) )
            return Tuple.Create(false, Array.Empty<string>());

        try
        {
            var coll = source.GetCollectionWithRetries<CachedValuesDoc>(collectionName + "-Meta");
            var docId = GetCachedDocId(fieldName);
            var doc = await coll.Find(Builders<CachedValuesDoc>.Filter.Eq(x => x.Id, docId)).FirstOrDefaultAsync(token);

            if ( doc == null || doc.Values == null )
                return Tuple.Create(false, Array.Empty<string>());

            var values = doc.Values.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var expireAtUtc = doc.ExpireAtUtc != default
                ? doc.ExpireAtUtc
                : (doc.CachedOnUtc == default ? DateTime.MinValue : doc.CachedOnUtc + ttl);
            var isStillValid = expireAtUtc > DateTime.UtcNow;
            return Tuple.Create(isStillValid, values);
        }
        catch
        {
            return Tuple.Create(false, Array.Empty<string>());
        }
    }

    public static async Task StoreCached(this MongoDbDataSource source, string collectionName, string fieldName, string[] values, TimeSpan ttl, CancellationToken token = default)
    {
        if ( values.Length == 0 || string.IsNullOrWhiteSpace(fieldName) || ttl <= TimeSpan.Zero )
            return;

        var cachedOnUtc = DateTime.UtcNow;
        var doc = new CachedValuesDoc
        {
            Id          = GetCachedDocId(fieldName),
            CachedOnUtc = cachedOnUtc,
            ExpireAtUtc = cachedOnUtc + ttl,
            Values      = values.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
        };

        if ( doc.Values.Length == 0 )
            return;

        try
        {
            var coll = source.GetCollectionWithRetries<CachedValuesDoc>(collectionName + "-Meta");
            await coll.ReplaceOneAsync(Builders<CachedValuesDoc>.Filter.Eq(x => x.Id, doc.Id), doc, new ReplaceOptions { IsUpsert = true }, token);
        }
        catch
        {
            // ignore invalid or incompatible cached document state
        }
    }

}
