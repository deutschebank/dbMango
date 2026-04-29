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
﻿using log4net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

internal static class FetchInfo
{
    public static long FetchId;
    public static int  ParallelFinds;
}


public abstract class MongoDbServiceBase<T> : IMongoDbService<T> where T : class
{
    // ReSharper disable StaticMemberInGenericType
    // ReSharper disable once InconsistentNaming
    protected static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);
    public static bool Quiet = false;
    // ReSharper restore StaticMemberInGenericType

    protected readonly IMongoCollection<T> Collection;
    private readonly   MongoDbSettings     _settings;


    public long Count => Collection.CountDocuments(x => true);

    public string CollectionName => Collection.CollectionNamespace.CollectionName;

    protected MongoDbServiceBase(
        MongoDbConfigRecord config,
        MongoDbSettings settings,
        string collectionName,
        string? databaseInstance = null)
    {
        _settings = settings;
        var db = MongoDbHelper.GetDatabase(config, settings, databaseInstance);
        Collection = db.GetCollection<T>(collectionName);
    }

    /// <summary>
    /// When inserting items into the collection, sometimes we have duplicates, so this method handles the duplicate items
    /// </summary>
    /// <param name="duplicateIds"></param>
    /// <param name="duplicateItems"></param>
    /// <returns></returns>
    protected abstract int ProcessDuplicates(HashSet<string> duplicateIds, List<T> duplicateItems);

    public IEnumerable<string> FindKeys(FilterDefinition<T> filter, CancellationToken token = default)
        => RetryHelper.DoWithRetries(() => FindKeysInternal(filter), 3, TimeSpan.FromSeconds(5), logFunc: LogMethod);

    public Task<int> InsertAsync(IEnumerable<T>    data, bool overrideExisting, bool suppressWarning = false,
                                 CancellationToken token = default) 
        => RetryHelper.DoWithRetriesAsync(() => InsertAsyncInternalAsync(data, overrideExisting, suppressWarning), 3, TimeSpan.FromSeconds(5), logFunc: LogMethod, token: token);

    public async IAsyncEnumerable<Dictionary<string, object>> AggregateAsync(
        string jsonPipeline, int maxFetchSize = -1, [EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var doc in AggregateInternalAsync(jsonPipeline, maxFetchSize, token))
        {
            yield return ConvertBsonToDictionary(doc);
        }
    }

    public IAsyncEnumerable<BsonDocument> AggregateAsyncRaw(string jsonPipeline,
                                                            int    maxFetchSize = -1, CancellationToken token = default)
        => AggregateInternalAsync(jsonPipeline, maxFetchSize, token);

    public Task ClearCOBAsync(DateTime cob, string? layer, string? book = null, string? root = null, CancellationToken token = default) 
        => RetryHelper.DoWithRetriesAsync(() => ClearCobInternalAsync(cob, layer, book, root, token), 3, TimeSpan.FromSeconds(5), logFunc: LogMethod, token: token);

    private const int ReportEveryNDocuments = 50_000;
    private const int NumberOfRetries       = 3;

    public IAsyncEnumerable<T> FindAsync(FilterDefinition<T>      filter,            bool allowRetries = true,
                                         ProjectionDefinition<T>? projection = null, int? limit = null, CancellationToken token = default)
        => allowRetries 
            ? FindAllowRetries(RenderToBsonDocument(filter).ToJson(), RenderToBsonDocument(projection)?.ToJson(), limit, token)
            : FindNoRetries(RenderToBsonDocument(filter).ToJson(), RenderToBsonDocument(projection)?.ToJson(), limit, token)
    ;

    public Task<long> CountAsync(FilterDefinition<T>      filter, CancellationToken token = default)
            => CountNoRetries(RenderToBsonDocument(filter).ToJson(), token)
    ;

    public async Task<BsonDocument> ExplainAsync(string command, CancellationToken token)
    {
        var explain = new BsonDocument
        {
            { "explain", BsonDocument.Parse(command) }
        };

        var res = await Collection.Database.RunCommandAsync(new BsonDocumentCommand<BsonDocument>(explain), cancellationToken: token);
        return res;
    }


    public static BsonDocument? RenderToBsonDocument(FilterDefinition<T>? filter)
    {
        if (filter == null)
            return null;
        var serializerRegistry = BsonSerializer.SerializerRegistry;
        var documentSerializer = serializerRegistry.GetSerializer<T>();
        var args               = new RenderArgs<T>(documentSerializer, serializerRegistry);
        return filter.Render(args);
    }

    public static BsonDocument? RenderToBsonDocument(ProjectionDefinition<T>? filter)
    {
        if (filter == null)
            return null;
        var serializerRegistry = BsonSerializer.SerializerRegistry;
        var documentSerializer = serializerRegistry.GetSerializer<T>();
        var args               = new RenderArgs<T>(documentSerializer, serializerRegistry);
        return filter.Render(args);
    }

    private async Task<long> CountNoRetries(string filter, CancellationToken token = default)
    {
        var options = new CountOptions
        {
            MaxTime         = _settings.MongoDbQueryTimeout
        };
        var count = await Collection.CountDocumentsAsync(filter, options, token);
        return count;
    }

    public async IAsyncEnumerable<T> FindNoRetries(string filter, string? projection = null, int? limit = null, [EnumeratorCancellation] CancellationToken token = default)
    {
        var processed = 0;
        var options = new FindOptions<T>
        {
            MaxTime         = _settings.MongoDbQueryTimeout,
            NoCursorTimeout = true,
            BatchSize       = _settings.MongoDbQueryBatchSize,
        };

        if (limit != null)
            options.Limit = limit.Value;

        if (projection != null)
            options.Projection = projection;

        var jsonFilter = filter
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("\t", " ")
                        .Replace("  ", " ")
            ;

        jsonFilter = jsonFilter.Length > 400
            ? jsonFilter[..400] + "..."
            : jsonFilter;

        var id = Interlocked.Increment(ref FetchInfo.FetchId);
        Interlocked.Increment(ref FetchInfo.ParallelFinds);
        try
        {
            var sw = Stopwatch.StartNew();

            if ( !Quiet ) 
                _log.Debug($"Id={id:000} Starting Find (no retries) Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Filter=\"{jsonFilter}\"");

            var cursor = await Collection.FindAsync(filter, options, token);

            if (sw.Elapsed > TimeSpan.FromSeconds(10))
                if ( !Quiet ) 
                    _log.Debug($"Id={id:000} Slow Find (no retries) Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Elapsed=\"{sw.Elapsed:g}\" Filter=\"{jsonFilter}\"");

            sw.Restart();

            var prevBatchElapsed = TimeSpan.Zero;

            while (true)
            {
                IEnumerable<T> batch;
                try
                {
                    if (!await cursor.MoveNextAsync(token))
                    {
                        if ( !Quiet ) 
                            _log.Debug($"Id={id:000} Find complete (no retries) Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Docs={processed} " +
                                   $"Elapsed=\"{sw.Elapsed:g}\" DocsSec={processed / (sw.ElapsedMilliseconds / 1000.0):0.00} Filter=\"{jsonFilter}\"");
                        yield break;
                    }
                    batch = cursor.Current;
                }
                catch (Exception e)
                {
                    var dps = processed / (sw.ElapsedMilliseconds / 1000.0);
                    throw new ApplicationException($"Id={id:000} Find (no retries) failed: {e.Message}. " +
                                                   $"Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Docs={processed} Elapsed=\"{sw.Elapsed:g}\" DocsSec={dps:0.00} Filter=\"{jsonFilter}\"", e);
                }

                foreach (var bson in batch)
                {
                    yield return bson;

                    processed += 1;

                    if ((processed % ReportEveryNDocuments) != 0)
                        continue;

                    var dps = ReportEveryNDocuments / ((sw.ElapsedMilliseconds - prevBatchElapsed.TotalMilliseconds) / 1000.0);
                    if ( !Quiet )
                        _log.Debug($"Id={id:000} Fetch {ReportEveryNDocuments:N0} documents (no retries) Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} " +
                               $"Elapsed=\"{sw.Elapsed - prevBatchElapsed:g}\" DocsSec={dps:0.00} (processed {processed:N0} so far)");
                    prevBatchElapsed = sw.Elapsed;
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref FetchInfo.ParallelFinds);
        }
    }

    public async IAsyncEnumerable<T> FindAllowRetries(string filter, string? projection = null, int? limit = null, [EnumeratorCancellation] CancellationToken token = default)
    {
        var       processed      = 0;
        var       attempts       = 0;
        Exception? firstException = null;
        var options = new FindOptions<T>
        {
            MaxTime         = _settings.MongoDbQueryTimeout,
            NoCursorTimeout = true,
            BatchSize       = _settings.MongoDbQueryBatchSize,
            Sort            = "{ _id: 1 }"
        };

        if (limit != null)
            options.Limit = limit.Value;

        if (projection != null)
            options.Projection = projection;

        var jsonFilter = filter
                        .Replace("\n", " ")
                        .Replace("\r", " ")
                        .Replace("\t", " ")
                        .Replace("  ", " ")
            ;

        var id = Interlocked.Increment(ref FetchInfo.FetchId);
        Interlocked.Increment(ref FetchInfo.ParallelFinds);
        try
        {

            var sw = Stopwatch.StartNew();

            if ( !Quiet ) 
                _log.Debug($"Id={id:000} Starting Find Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Filter=\"{jsonFilter}\"");

            //var coll = Collection.Database.GetCollection<BsonDocument>(CollectionName);

            //default is 10, prevent the cursor expiring between calling MoveNext()
            //unrestricted, it appears batch sizes are ~10K when running locally

            //https://stackoverflow.com/questions/44248108/mongodb-error-getmore-command-failed-cursor-not-found

            //the cursor returns batches, so iterate through each batch 
            while (true)
            {
                options.Skip = processed;
                var cursor = await Collection.FindAsync(filter, options, token);

                if (sw.Elapsed > TimeSpan.FromSeconds(5))
                    _log.Debug($"Id={id:000} Slow Find Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Elapsed=\"{sw.Elapsed:g}\" Filter=\"{jsonFilter}\"");

                sw.Restart();

                var prevBatchElapsed = TimeSpan.Zero;

                while (true)
                {
                    IEnumerable<T> batch;
                    try
                    {
                        if (!await cursor.MoveNextAsync(token))
                        {
                            yield break;
                        }
                        batch = cursor.Current;
                    }
                    catch (Exception e)
                    {
                        attempts++;

                        _log.Warn($"Id={id:000} Exception when calling Find Attempt={attempts}/3 Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Processed={processed} Filter=\"{jsonFilter}\"", e);

                        firstException ??= e;

                        if (attempts == NumberOfRetries)
                        {
                            var dps = processed / (sw.ElapsedMilliseconds / 1000.0);
                            throw new ApplicationException($"Id={id:000} Find failed: {firstException.Message}. " +
                                                           $"Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} Docs={processed} Elapsed=\"{sw.Elapsed:g}\" DocsSec={dps:0.00} Filter=\"{jsonFilter}\"", firstException);
                        }

                        //pause before trying again
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                        break; //return back to the while loop
                    }

                    foreach (var bson in batch)
                    {
                        yield return bson;

                        processed++;

                        if ((processed % ReportEveryNDocuments) != 0)
                            continue;

                        var dps = ReportEveryNDocuments / ((sw.ElapsedMilliseconds - prevBatchElapsed.TotalMilliseconds) / 1000.0);
                        if ( !Quiet ) 
                            _log.Debug($"Id={id:000} Fetch {ReportEveryNDocuments:N0} documents Collection=\"{CollectionName}\" Concurrency={FetchInfo.ParallelFinds} " +
                                   $"Elapsed=\"{sw.Elapsed - prevBatchElapsed:g}\" DocsSec={dps:0.00} (processed {processed:N0} so far)");
                        prevBatchElapsed = sw.Elapsed;

                    }
                }
            }
        }
        finally
        {
            Interlocked.Decrement(ref FetchInfo.ParallelFinds);
        }
    }

    public void DeleteCollection() => RetryHelper.DoWithRetries(() => Collection.Database.DropCollection(Collection.CollectionNamespace.CollectionName), 3, TimeSpan.FromSeconds(5));

    public void UpdateOne(FilterDefinition<T> filter, UpdateDefinition<T> update, CancellationToken token = default) 
        => RetryHelper.DoWithRetries(() => Collection.UpdateOne(filter, update), 3, TimeSpan.FromSeconds(5));

    public void ReplaceOne(FilterDefinition<T> filter, T doc, CancellationToken token) => RetryHelper.DoWithRetries(() 
        => ReplaceOne(filter, doc, token), 3, TimeSpan.FromSeconds(5));

    #region private methods
    private IEnumerable<string> FindKeysInternal(FilterDefinition<T> filter)
    {
        var projection = BsonSerializer.Deserialize<BsonDocument>("{ _id : 1}");

        var ids = Collection
                 .Find(filter)
                 .Project(projection)
                 .ToEnumerable()
                 .Select(doc => doc[0].AsString)
                 .ToList();
        return ids;
    }

    private async Task<int> InsertAsyncInternalAsync(IEnumerable<T> data, bool overrideExisting, bool suppressWarning)
    {
        var ignored = 0;

        var options = new InsertManyOptions
        {
            BypassDocumentValidation = true,
            IsOrdered                = false
        };

        var dataList = data.ToList();
        if (dataList.Count == 0)
            return 0;

        try
        {
            await Collection.InsertManyAsync(dataList, options);
        }
        catch (Exception ex)
        {
            if (MongoDbHelper.IsDuplicateKeyError(ex))
            {
                var toProcess = MongoDbHelper.ExtractDuplicateKeys(ex);
                if (!overrideExisting)
                {
                    if ( !suppressWarning )
                        _log.Warn($"Duplicate entries found and ignored. DataCount={dataList.Count} DuplicateCount={toProcess.Count} DocumentClass=\"{typeof(T).Name}\" Collection=\"{Collection.CollectionNamespace.CollectionName}\"");
                    ignored = toProcess.Count;
                }
                else
                    ProcessDuplicates(toProcess, dataList);
            }
            else
                throw;
        }

        //if ( !Quiet ) _log.Debug($"Collection=\"{Collection.CollectionNamespace.CollectionName}\" Inserted={dataList.Count - replaced - ignored} Replaced={replaced} Ignored={ignored} of Total={dataList.Count}");
        return dataList.Count - ignored;
    }

    private async Task ClearCobInternalAsync(DateTime cob, string? layer, string? book = null, string? root = null, CancellationToken token = default)
    {
        var layerName = string.IsNullOrWhiteSpace(root)
            ? "Layer"
            : root + ".Layer";
        var bookName = string.IsNullOrWhiteSpace(root)
            ? "Book"
            : root + ".Book";
        var cobName = string.IsNullOrWhiteSpace(root)
            ? "COB"
            : root + ".COB";

        FilterDefinition<T> filter;

        if (!string.IsNullOrWhiteSpace(layer) && !string.IsNullOrWhiteSpace(book))
            filter = new FilterDefinitionBuilder<T>().And(
                new FilterDefinitionBuilder<T>().Eq(cobName,   cob.Date),
                new FilterDefinitionBuilder<T>().Eq(layerName, layer),
                new FilterDefinitionBuilder<T>().Eq(bookName,  book)
            );
        else if (!string.IsNullOrWhiteSpace(layer))
            filter = new FilterDefinitionBuilder<T>().And(
                    new FilterDefinitionBuilder<T>().Eq(cobName,   cob.Date),
                    new FilterDefinitionBuilder<T>().Eq(layerName, layer)
                )
                ;
        else filter = !string.IsNullOrWhiteSpace(book)
            ? new FilterDefinitionBuilder<T>().And(
                    new FilterDefinitionBuilder<T>().Eq(cobName,  cob.Date),
                    new FilterDefinitionBuilder<T>().Eq(bookName, book)
                )
            : new FilterDefinitionBuilder<T>().Eq(cobName, cob.Date);

        var result = await Collection.DeleteManyAsync(filter, token);
        if ( !Quiet ) 
            _log.Debug($"Collection=\"{Collection.CollectionNamespace.CollectionName}\" Deleted={result.DeletedCount} Book=\"{book}\" COB=\"{cob:yyyy-MM-dd}\" Layer=\"{layer}\"");
    }

    public static Dictionary<string, object> ConvertBsonToDictionary(BsonDocument doc)
    {
        var res = doc.ToDictionary();

        // for map/reduce results
        var value = res.TryGetValue("value", out var re)
            ? (Dictionary<string, object>)re
            : null;

        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var data in res.Where(x => x.Key != "_id" && x.Key != "value"))
        {
            result[data.Key] = data.Value;
        }

        if (value != null)
        {
            foreach (var data in value)
            {
                result[data.Key] = data.Value;
            }
        }

        if (res["_id"] is not Dictionary<string, object> id)
            return result;

        foreach (var data in id)
        {
            result[data.Key] = data.Value;
        }

        return result;
    }

    private async IAsyncEnumerable<BsonDocument> AggregateInternalAsync(
        string jsonPipeline, 
        int maxFetchSize,
        [EnumeratorCancellation] CancellationToken token = default
        )
    {
        var options = new AggregateOptions
        {
            BypassDocumentValidation = true,
            AllowDiskUse             = true,
        };

        var pipeline = BsonSerializer.Deserialize<BsonArray>(jsonPipeline).Select(p => p.AsBsonDocument).ToList();

        using var cursor = await Collection.AggregateAsync<BsonDocument>(pipeline, options, token);
        var       docs   = 0;

        while (await cursor.MoveNextAsync(token))
        {
            var batch = cursor.Current;
            foreach (var doc in batch)
                yield return doc;

            if ( maxFetchSize > 0 && docs++ >= maxFetchSize)
                yield break;
        }
    }

    /// <summary>
    /// Log method for retry function, called for each iteration
    /// </summary>
    /// <param name="iteration"></param>
    /// <param name="maxRetries"></param>
    /// <param name="e"></param>
    private void LogMethod(int iteration, int maxRetries, Exception e)
    {
        if (iteration < maxRetries - 1)
            _log.Warn($"MongoDB error, retrying  RetriesLeft={maxRetries-iteration-1}, Collection=\"{Collection.CollectionNamespace.CollectionName}\"", e);
        else
            _log.Error($"MongoDB error, retries exhausted, Collection=\"{Collection.CollectionNamespace.CollectionName}\"", e);
    }

    /// <summary>
    /// Delete documents matching the filter
    /// </summary>
    /// <param name="filter">Filter selecting a single document</param>
    /// <param name="token"></param>
    public async Task<long> Delete(FilterDefinition<T> filter, CancellationToken token = default)
    {
        var result = await Collection.DeleteManyAsync(filter, token);
        if ( !Quiet ) 
            _log.Debug($"Collection=\"{Collection.CollectionNamespace.CollectionName}\" Deleted={result.DeletedCount} Filter:\n{filter.ToJson(new () {Indent = true})}");
        return result.DeletedCount;
    }


#endregion private methods
}