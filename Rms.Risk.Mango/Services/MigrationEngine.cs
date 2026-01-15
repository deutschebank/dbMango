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
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Rms.Risk.Mango.Controllers;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Pivot.UI.Services;
using Rms.Risk.Mango.Services.Context;
using Rms.Risk.Mango.Services.Security;
using Rms.Service.Bootstrap.Security;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Security.Claims;
using log4net;
using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services;

public class MigrationEngine(
    // ReSharper disable InconsistentNaming
    // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
    IDatabaseConfigurationService _databases, 
    IAuthorizationService         _auth, 
    IMongoDbServiceFactory        _factory,
    ITempFileStorage              _storage,
    IPasswordManager              _passwordManager, 
    ISingleUseTokenService        _singleUseTokenService,
    IOptions<DbMangoSettings>     _settings
    // ReSharper restore InconsistentNaming
) : IMigrationEngine
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    private class InternalMigrationJobStatus(MigrationJob job)
    {
        public MigrationJob            Status { get; } = job;
        public CancellationTokenSource Cts    { get; } = new();
    }

    private readonly ConcurrentDictionary<string, InternalMigrationJobStatus> _jobs    = [];
    private readonly ConcurrentBag<MigrationJob>                              _allJobs = [];

    public List<MigrationJob> List() => _allJobs.OrderBy(x => x.JobId).ToList();

    public async Task Add(MigrationJob job, ClaimsPrincipal user)
    {
        if (_jobs.TryGetValue(job.DestinationDatabase, out var existing ))
            throw new($"Only one migration job allowed for destination {job.DestinationDatabase}. Migration already initiated by {existing.Status.Email}.");

        await CheckJobAccess(job, user);


        var newJob = new InternalMigrationJobStatus(job);

        if ( !_jobs.TryAdd(job.DestinationDatabase, newJob) )
            throw new($"Only one migration job allowed for destination {job.DestinationDatabase}.");
        
        _allJobs.Add(job);
        _ = Task.Run(() => RunJob(job, newJob.Cts.Token));
    }

    private async Task CheckJobAccess(MigrationJob job, ClaimsPrincipal user, bool forCancel = false)
    {
        if (!_databases.Databases.ContainsKey(job.SourceDatabase))
            throw new($"Source database {job.SourceDatabase} is not defined");

        if (!_databases.Databases.ContainsKey(job.DestinationDatabase))
            throw new($"Destination database {job.DestinationDatabase} is not defined");

        var enableSource = await _auth.AuthorizeAsync(
            user, 
            job.SourceDatabase, 
            [new ReadAccessRequirement()]);

        if (!enableSource.Succeeded)
            throw new("Read access to the source database required");

        if ( job.Type != MigrationJob.JobType.Download )
        {
            var enableDest = await _auth.AuthorizeAsync(
                user, 
                job.DestinationDatabase, 
                [new WriteAccessRequirement()]);

            if (!enableDest.Succeeded)
                throw new("Write access to the destination database required");
        }

        if ( forCancel)
        {
            var enableCancel = await _auth.AuthorizeAsync(
                user, 
                job.DestinationDatabase, 
                [new AdminAccessRequirement()]);

            if (!enableCancel.Succeeded)
                throw new("Admin access to the destination database required");
        }
    }

    public async Task Cancel(MigrationJob job, ClaimsPrincipal user)
    {
        await CheckJobAccess(job, user, true);
        
        if (!_jobs.TryGetValue(job.DestinationDatabase, out var existing ))
            throw new($"No migration job registered for {job.DestinationDatabase}");
        if ( existing.Status.JobId != job.JobId )
            throw new($"Migration job for {job.DestinationDatabase} have JobId=\"{existing.Status.JobId:D}\", but received {job.JobId:D}");

        await existing.Cts.CancelAsync();
    }

    private async Task RunJob(MigrationJob job, CancellationToken token)
    {
        var audit = _factory.CreateAudit(job.DestinationDatabase, job.DestinationDatabaseInstance);
        try
        {
            job.StartedAtUtc = DateTime.UtcNow;
            var auditRec = CreateAuditRecord(job, $"ticket: {job.Ticket} email: {job.Email} Migration job ({job.Type.ToString()}) starting...");
            await audit.Record(auditRec, token);

            switch ( job.Type)
            {
                case MigrationJob.JobType.Copy:
                    foreach (var collStatus in job.Status)
                    {
                        try
                        {
                            collStatus.StartedAtUtc = DateTime.UtcNow;
                            await MigrateOneCollection(job, collStatus, token);
                            collStatus.FinishedAtUtc = DateTime.UtcNow;
                        }
                        catch (Exception ex)
                        {
                            collStatus.FinishedAtUtc = DateTime.UtcNow;
                            collStatus.Complete      = true;
                            collStatus.Exception     = ex;
                            job.Exception            = ex;
                        }
                    }
                    break;
                case MigrationJob.JobType.Download:
                    await DownloadCollections(job, token);
                    break;
                case MigrationJob.JobType.Upload:
                    await UploadCollectionsFromZip(job, token);
                    break;
                default:
                    throw new($"Unsupported migration job type: {job.Type}");
            }

        }
        catch (Exception ex)
        {
            job.Exception = ex;
        }
        finally
        {
            job.Complete      = true;
            job.FinishedAtUtc = DateTime.UtcNow;

            _jobs.TryRemove(job.DestinationDatabase, out _);

            var auditRec = token.IsCancellationRequested
                ? CreateAuditRecord(job, $"ticket: {job.Ticket} email: {job.Email} Migration job ({job.Type.ToString()}) cancelled.")
                : CreateAuditRecord(job, $"ticket: {job.Ticket} email: {job.Email} Migration job ({job.Type.ToString()}) complete.");

            await audit.Record(auditRec, token);
        }
    }

    private static AuditRecord CreateAuditRecord(MigrationJob job, string comment)
        =>  new (
            job.DestinationDatabase,
            DateTime.UtcNow,
            job.Email,
            job.Ticket,
            job.Exception == null,
            CreateAuditCommand(job, comment)
        );

    private static BsonDocument CreateAuditCommand(MigrationJob job, string comment)
    {
        return new ()
        {
            ["dbMangoMigrate"]  = job.ToString(),
            ["type"]            = job.Type.ToString(),
            ["from"]            = job.SourceDatabase,
            ["to"]              = job.DestinationDatabase,
            ["upsert"]          = job.Upsert,
            ["clearDestBefore"] = job.ClearDestinationBefore,
            ["disableIndexes"]  = job.DisableIndexes,
            ["batchSize"]       = job.BatchSize,
            ["ticket"]          = job.Ticket,
            ["collections"] = new BsonArray(job.Status.Select(x => new BsonDocument
            {
                ["sourceCollection"]      = x.SourceCollection,
                ["destinationCollection"] = x.DestinationCollection,
                // ReSharper disable UseCollectionExpression
                ["filter"]     = x.Filter ?? new BsonDocument(),
                ["projection"] = x.Projection ?? new BsonDocument(),
                // ReSharper restore UseCollectionExpression
                ["count"]  = x.Count,
                ["copied"] = x.Copied,
                ["error"]  = x.Error
            })),
            ["complete"] = job.Complete,
            ["error"]    = job.Error,
            ["comment"]  = comment
        };
    }

    private async Task UploadCollectionsFromZip(MigrationJob job, CancellationToken token)
    {
        job.StartedAtUtc = DateTime.UtcNow;
        try
        {
            await using var zipStream = new FileStream(job.UploadedFileName, FileMode.Open, FileAccess.Read);

            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var needToUpload = job.Status.Select( x => x.SourceCollection ).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var groupedChunks = GroupAndChunkEntriesByFolder(archive.Entries, job.BatchSize, needToUpload);

            foreach ( var (coll,chunks) in groupedChunks ) 
            {   
                var collStatus = job.Status.FirstOrDefault(x => x.SourceCollection.Equals(coll, StringComparison.OrdinalIgnoreCase));
                if ( collStatus == null )
                    continue; // no collection status found for this collection

                collStatus.Copied       = 0;
                collStatus.Count        = chunks.Sum(x => x.Count);
                collStatus.StartedAtUtc = DateTime.UtcNow;

                try
                {

                    var filter = collStatus.Filter == null
                        ? "{ _id : { $ne: \"\" } }"
                        : collStatus.Filter.ToString()!;

                    if ( job.ClearDestinationBefore )
                    {
                        await ClearDestination(job, collStatus, filter, token);
                    }

                    var destination = _factory.Create(
                        job.DestinationDatabase, 
                        collStatus.SourceCollection,
                        job.DestinationDatabaseInstance 
                        );


                    foreach (var chunk in chunks)
                    {
                        var documents = new List<BsonDocument>();

                        foreach (var entryName in chunk)
                        {
                            var entry = archive.GetEntry(entryName);
                            if (entry == null)
                                continue;

                            await using var entryStream = entry.Open();
                            using var reader = new StreamReader(entryStream);
                            var content = await reader.ReadToEndAsync(token);

                            var bsonDocument = BsonDocument.Parse(content);
                            documents.Add(bsonDocument);
                        }

                        if (documents.Count > 0)
                        {
                            var insertedCount = await destination.InsertAsync(documents, job.Upsert, true, token);

                            // number of the documents inserted can be different if either using Upsert, inserting into non-cleared collection
                            // or using a filter(?)
                            lock (_lock)
                            {
                                collStatus.Copied += insertedCount;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    job.Exception = ex;
                }

                collStatus.FinishedAtUtc = DateTime.UtcNow;
                collStatus.Complete      = true;
            }
        }
        catch (Exception ex)
        {
            job.Exception = ex;
        }
        job.FinishedAtUtc = DateTime.UtcNow;
        job.Complete      = true;
    }

    private async Task DownloadCollections(MigrationJob job, CancellationToken token)
    {
        var zipFilePath = _storage.GetTempFileName( $"{job.JobId}_data.zip" );

        await using var zipStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

        foreach (var collStatus in job.Status)
        {
            collStatus.Copied       = 0;
            collStatus.StartedAtUtc = DateTime.UtcNow;

            try
            {
                token.ThrowIfCancellationRequested();

                var source = _factory.Create(job.SourceDatabase, collStatus.SourceCollection, job.SourceDatabaseInstance);
                var filter = collStatus.Filter?.ToString() ?? "{ _id : { $ne: \"\" } }";

                archive.CreateEntry($"{collStatus.SourceCollection}/");

                var count = await source.CountAsync(filter, token);
                lock (_lock)
                {
                    collStatus.Count = count;
                }

                var copied = 0;

                await foreach (var doc in source.FindAsync(filter, false, collStatus.Projection?.ToString(), limit: null, token))
                {
                    token.ThrowIfCancellationRequested();

                    var fileName = MakeFileName(doc["_id"]);
                    var fileEntry = archive.CreateEntry($"{collStatus.SourceCollection}/{fileName}");

                    await using var entryStream = fileEntry.Open();
                    await using var writer = new StreamWriter(entryStream);

                    await writer.WriteAsync(doc.ToJson(new() { Indent = job.PrettyPrint }));

                    copied += 1;
                    if ( copied % 500 == 0 )
                    {
                        lock( _lock)
                        {
                            collStatus.Copied += copied;
                            copied = 0;
                        }
                    }
                }

                lock( _lock)
                {
                    collStatus.Copied += copied;
                }
            }
            catch (Exception ex)
            {
                collStatus.Exception = ex;
                job.Exception        = ex;
            }

            collStatus.FinishedAtUtc = DateTime.UtcNow;
            collStatus.Complete      = true;
        }

        job.FinishedAtUtc = DateTime.UtcNow;
        job.Complete      = true;
        job.DownloadUrl        = DownloadController.GetDownloadLink(_passwordManager, _singleUseTokenService, zipFilePath, "dbMango_data.zip");
    }

    private static long _counter;

    private static string MakeFileName(BsonValue bsonValue)
    {
        var fileName = (bsonValue.ToString() ?? "")
               .Replace("..", ".")
               .Replace("{",  "")
               .Replace("}",  "")
               .Replace("[",  "")
               .Replace("]",  "")
               .Replace("\"", "")
               .Replace("'",  "")
               .Replace("?",  "")
               .Replace("*",  "")
               .Replace("\\", "")
               .Replace("/", "")
               .Replace(":", "")
            ;

        if ( string.IsNullOrWhiteSpace(fileName) || fileName.Length > 100)
            fileName = $"{Interlocked.Increment(ref _counter):D10}";

        return $"{fileName}.json";
    }

    private async Task MigrateOneCollection(MigrationJob job, MigrationJob.CollectionJob collStatus, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        collStatus.Copied    = 0;
        collStatus.Complete  = false;
        collStatus.Exception = null;

        var filter = collStatus.Filter == null
            ? "{ }"
            : collStatus.Filter.ToString()!;

        var loadTask  = LoadIds(job, collStatus, filter, token);
        var clearTask = ClearDestination(job, collStatus, filter, token);

        await Task.WhenAll( loadTask, clearTask );
        
        var indexes = await DisableIndexes(job, collStatus, token);

        try
        {
            var ids = loadTask.Result;

            // parallel inserts

            collStatus.StartedAtUtc = DateTime.UtcNow; // reset start time for correct DPS

            var options = new ParallelOptions
            {
                CancellationToken = token,
                MaxDegreeOfParallelism = job.MaxDegreeOfParallelism
            };

            await Parallel.ForEachAsync(
                ids.Chunk(job.BatchSize), 
                options, 
                async (x,t) => await ProcessBatch(x, job, collStatus, t)
            );

        }
        catch (Exception ex)
        {
            collStatus.Exception = ex;
            job.Exception        = ex;
        }
        finally
        {
            if ( indexes.Length > 0 )
            {
                // can't use token here as it can be already cancelled
                // using very long timeout to ensure indexes are created
                var cts3 = new CancellationTokenSource(TimeSpan.FromMinutes(60));
                await EnableIndexes(indexes, job, collStatus, cts3.Token);
            }
        }
        collStatus.Complete     = true;
        
    }

    private async Task<DatabaseStructureLoader.IndexStructure[]> DisableIndexes(MigrationJob job, MigrationJob.CollectionJob collStatus, CancellationToken token)
    {
        if ( !job.DisableIndexes)
            return [];

        var config = MongoDbServiceFactory.GetConfig(_databases.Databases[job.DestinationDatabase].Config, job.DestinationDatabaseInstance);
        IMongoDbDatabaseAdminService mongo = new MongoDbDatabaseAdminService(config, _settings.Value.Settings, job.DestinationDatabaseInstance);
        
        var indexes = (await DatabaseStructureLoader.LoadIndexes(
            mongo, 
            collStatus.DestinationCollection,
            token
        ))
        .Where(x => !IsPrimaryIndex(x) )
        .ToArray();

        try
        {
            var command = new BsonDocument
            {
                ["dropIndexes"] = collStatus.DestinationCollection,
                ["index"]   = new BsonArray(indexes.Select(x => x.Name))
            };

            await mongo.RunCommand(command, token);
        }
        catch (Exception ex)
        {
            collStatus.Exception = ex;
            job.Exception        = ex;
            throw;
        }

        return indexes;
    }

    private async Task EnableIndexes(DatabaseStructureLoader.IndexStructure[] indexes, MigrationJob job, MigrationJob.CollectionJob collStatus, CancellationToken token)
    {
        if ( !job.DisableIndexes || indexes.Length == 0)
            return;

        try
        {
            var config = MongoDbServiceFactory.GetConfig(_databases.Databases[job.DestinationDatabase].Config, job.DestinationDatabaseInstance);
            var mongo = new MongoDbDatabaseAdminService(config, _settings.Value.Settings, job.DestinationDatabaseInstance);

            await DatabaseStructureLoader.CreateIndexes(mongo, collStatus.DestinationCollection, indexes, token);
        }
        catch (Exception ex)
        {
            collStatus.Exception = ex;
            job.Exception        = ex;
            throw;
        }
    }

    private static bool IsPrimaryIndex(DatabaseStructureLoader.IndexStructure indexStructure)
    {
        if ( indexStructure.Name == "_id_" )
            return true;
        if ( indexStructure.Key.Count == 1 && indexStructure.Key[0].Key == "_id" )
            return true;

        return false;
    }

    private readonly Lock _lock = new();

    private async Task<List<BsonValue>> LoadIds(
        MigrationJob               job,
        MigrationJob.CollectionJob collStatus, 
        string                     filter,
        CancellationToken          token
        )
    {
        var source = _factory.Create(job.SourceDatabase, collStatus.SourceCollection, job.SourceDatabaseInstance);

        var sw = Stopwatch.StartNew();
        var ids    = await ExtractIDs(source, filter, token);
        sw.Stop();
        _log.Debug($"Loading Count={ids.Count} IDs took Elapsed=\"{sw.Elapsed}\" DPS={1000.0 * ids.Count / sw.ElapsedMilliseconds:N0}");

        lock (_lock)
            collStatus.Count = ids.Count;
        
        return ids;
    }

    private async Task ClearDestination(
        MigrationJob               job,
        MigrationJob.CollectionJob collStatus, 
        string                     filter,
        CancellationToken          token
        )
    {
        if (!job.ClearDestinationBefore)
            return;

        collStatus.Cleared = 0;

        var destCollection = string.IsNullOrWhiteSpace(collStatus.DestinationCollection)
                ? collStatus.SourceCollection
                : collStatus.DestinationCollection
            ;

        var destination = _factory.Create(job.DestinationDatabase, destCollection, job.DestinationDatabaseInstance);

        collStatus.Cleared = await destination.Delete(filter, token);
    }

    private async Task ProcessBatch(
        BsonValue[]                   ids,
        MigrationJob                  job,
        MigrationJob.CollectionJob    collStatus, 
        CancellationToken             token
        )
    {
        var source = _factory.Create(job.SourceDatabase, collStatus.SourceCollection, job.SourceDatabaseInstance);

        var filter = new BsonDocument
        {
            ["_id"] = new BsonDocument()
            {
                ["$in"] = new BsonArray(ids)
            }
        };

        var destCollection = string.IsNullOrWhiteSpace(collStatus.DestinationCollection)
                ? collStatus.SourceCollection
                : collStatus.DestinationCollection
            ;

        var destination = _factory.Create(job.DestinationDatabase, destCollection, job.DestinationDatabaseInstance);

        var batch =new List<BsonDocument>();

        var readSw = Stopwatch.StartNew();

        await foreach (var doc in source
                          .FindAsync(filter, false, collStatus.Projection?.ToString(), limit: null, token)
                      )
        {
            token.ThrowIfCancellationRequested();

            batch.Add(doc);
        }

        readSw.Stop();

        if (batch.Count <= 0)
            return;

        token.ThrowIfCancellationRequested();

        var writeSw = Stopwatch.StartNew();

        var c = await destination.InsertAsync(batch, job.Upsert, true, token: token);

        writeSw.Stop();

        lock (_lock)
        {
            collStatus.Copied         += c;
            collStatus.TotalReadMSec  += readSw.ElapsedMilliseconds;
            collStatus.TotalWriteMSec += writeSw.ElapsedMilliseconds;
        }
    }

    private static async Task<List<BsonValue>> ExtractIDs(
        IMongoDbService<BsonDocument> source, 
        string                        filter,
        CancellationToken             token
        )
    {
        token.ThrowIfCancellationRequested();

        var projection ="{ _id : 1 }";

        var batch = new List<BsonValue>();

        await foreach (var doc in source
                          .FindAsync(filter, false, projection, limit: null, token)
                      )
        {
            token.ThrowIfCancellationRequested();

            var id = doc["_id"];
            batch.Add(id);
        }

        return batch;
    }
    private Dictionary<string, List<List<string>>> GroupAndChunkEntriesByFolder(
        IEnumerable<ZipArchiveEntry> entries, int batchSize, HashSet<string> needToUpload)
    {
        var groupedChunks = new Dictionary<string, List<List<string>>>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            var folder = Path.GetDirectoryName(entry.FullName) ?? string.Empty;
            folder = folder.TrimEnd('/').TrimEnd('\\');

            if ( !needToUpload.Contains(folder) )
                continue;

            if (!groupedChunks.TryGetValue(folder, out var chunks))
            {
                chunks                = new();
                groupedChunks[folder] = chunks;
            }

            var fileName = Path.GetFileName(entry.FullName);
            if (string.IsNullOrWhiteSpace(fileName))
                continue;

            if (chunks.Count == 0 || chunks.Last().Count >= batchSize)
            {
                chunks.Add(new());
            }

            chunks.Last().Add(entry.FullName);
        }

        return groupedChunks;
    }
}