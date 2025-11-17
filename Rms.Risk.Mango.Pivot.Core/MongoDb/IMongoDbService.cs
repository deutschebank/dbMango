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

/// <summary>
/// Non templated portion of mongo interface 
/// </summary>
public interface IGenericMongoDbServices
{
    /// <summary>Name of the collection this instance operates on</summary>
    string CollectionName { get; }
    
    /// <summary>Number of records in the collection</summary>
    long Count { get; }

    /// <summary>
    /// Delete the entire collection. For temporary collections only!
    /// </summary>
    void DeleteCollection();

    /// <summary>
    /// Run aggregate query async. No support for reties.
    /// </summary>
    /// <param name="jsonPipeline">Aggregation pipeline query (JSON)</param>
    /// <param name="maxFetchSize">Maximum number of documents fetched. Use -1 for no limit.</param>
    /// <param name="token"></param>
    /// <returns></returns>
    IAsyncEnumerable<Dictionary<string, object>> AggregateAsync(string jsonPipeline, int maxFetchSize = -1, CancellationToken token = default);

    /// <summary>
    /// Run aggregate query async. No support for reties.
    /// </summary>
    /// <param name="jsonPipeline">Aggregation pipeline query (JSON)</param>
    /// <param name="maxFetchSize">Maximum number of documents fetched. Use -1 for no limit.</param>
    /// <param name="token"></param>
    /// <returns></returns>
    IAsyncEnumerable<BsonDocument> AggregateAsyncRaw(string jsonPipeline, int maxFetchSize = -1, CancellationToken token = default);

    /// <summary>
    /// Explain the command execution plan
    /// </summary>
    Task<BsonDocument> ExplainAsync(string command, CancellationToken token = default);

    /// <summary>
    /// Clear all data for given COB/layer/book/root
    /// </summary>
    /// <param name="cob"></param>
    /// <param name="layer"></param>
    /// <param name="book"></param>
    /// <param name="root">???</param>
    /// <param name="token"></param>
    Task ClearCOBAsync(DateTime cob, string? layer = null, string? book = null, string? root = null, CancellationToken token = default);
}

/// <summary>
/// MongoDB interface for collections where all documents have type T (generic argument)
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IMongoDbService<T> : IGenericMongoDbServices
{
    /// <summary>
    /// Insert documents into collection
    /// </summary>
    /// <param name="data">Data to insert</param>
    /// <param name="overrideExisting">If true data will replace existing documents with the same keys</param>
    /// <param name="suppressWarning"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<int> InsertAsync(IEnumerable<T> data, bool overrideExisting, bool suppressWarning = false, CancellationToken token = default);

    /// <summary>
    /// Get documents matching the filter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="allowRetries">Enables retry logic. Warning: it sorting the results. Do not use for large result sets!</param>
    /// <param name="projection">Optional projection definition</param>
    /// <param name="limit">Max number of results returned</param>
    /// <param name="token"></param>
    /// <returns></returns>
    IAsyncEnumerable<T> FindAsync(FilterDefinition<T> filter, bool allowRetries = true, ProjectionDefinition<T>? projection = null, int? limit = null, CancellationToken token = default);

    /// <summary>
    /// Get documents matching the filter
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken token = default);

    /// <summary>
    /// Get only keys matching the filter (not documents!)
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    IEnumerable<string> FindKeys(FilterDefinition<T> filter, CancellationToken token = default);

    /// <summary>
    /// Update document(s)
    /// </summary>
    /// <param name="filter">Filter</param>
    /// <param name="update">Update definition</param>
    /// <param name="token"></param>
    void UpdateOne( FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken token = default );

    /// <summary>
    /// Replace document
    /// </summary>
    /// <param name="filter">Filter selecting a single document</param>
    /// <param name="doc">New value</param>
    /// <param name="token"></param>
    void ReplaceOne( FilterDefinition<T> filter, T doc, CancellationToken token = default );

    /// <summary>
    /// Delete documents matching the filter
    /// </summary>
    /// <param name="filter">Filter selecting a single document</param>
    /// <param name="token"></param>
    /// <returns>Number of deleted documents</returns>
    Task<long> Delete( FilterDefinition<T> filter, CancellationToken token = default );
}