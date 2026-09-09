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
using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services;

public class SchemaLoader(
    // ReSharper disable InconsistentNaming
    IUserSession _userSession,
    ILogger<SchemaLoader> _logger
    // ReSharper restore InconsistentNaming
    ) : ISchemaLoader
{
    private static readonly TimeSpan _cacheTtl = TimeSpan.FromHours(2);

    private static readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private static readonly Lock _cacheLock = new();

    private sealed record CacheEntry(BsonDocument Schema, DateTimeOffset ExpiresAt);

    public async Task<BsonDocument?> LoadSchema(string collection, CancellationToken token = default)
    {
        using var timeoutCts = token == CancellationToken.None
                ? new CancellationTokenSource(TimeSpan.FromSeconds(30))
                : null
            ;

        var effectiveToken = timeoutCts?.Token ?? token;

        var cacheKey = GetCacheKey();
        var now = DateTimeOffset.UtcNow;

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cacheEntry))
            {
                if (cacheEntry.ExpiresAt > now)
                    _cache.Remove(cacheKey);
                else
                    return cacheEntry.Schema;
            }
        }

        var schema = 
            await LoadSchemaFromMeta(collection, effectiveToken)
            ?? await InferSchema(collection, effectiveToken);

        if ( schema == null)
            return null;

        lock (_cacheLock)
        {
            _cache[cacheKey] = new CacheEntry(schema, DateTimeOffset.UtcNow.Add(_cacheTtl));
        }

        return schema;

    }

    private async Task<BsonDocument?> LoadSchemaFromMeta(string collection, CancellationToken token)
    {
        try
        {
            var metaCollection = collection + "-Meta";

            var admin = _userSession.MongoDbAdmin;

            var collections = await admin.ListCollections(token);
            if (!collections.Contains(metaCollection))
                return null;

            var metaService = _userSession.GetCustomMongoDbService(_userSession.Database, _userSession.DatabaseInstance, metaCollection);

            var filter = Builders<BsonDocument>.Filter.Eq("_id", "Schema");

            await foreach (var doc in metaService.FindAsync(filter, allowRetries: false, projection: null, limit: 1, token: token))
            {
                return doc;
            }

            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to load schema from meta collection '{meta}' for collection '{collectionName}'", collection + "-Meta", collection);
            return null;
        }
    }

    private async Task<BsonDocument?> InferSchema(string collection, CancellationToken token)
    {
        BsonDocument schema;
        try
        {
            _userSession.Collection = collection; // Ensure the collection is set for schema inference
            schema = await JsonSchemaExtractor.InferJsonSchema(
                _userSession.MongoDb,
                token: token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log the error and return null if schema inference fails
            _logger.LogError(ex, "Failed to load schema for collection '{collectionName}': {message}", _userSession.Collection, ex.Message);
            return null;
        }

        return schema;
    }

    private string GetCacheKey() =>
        string.Join("|", _userSession.Database, _userSession.DatabaseInstance, _userSession.Collection);
}

