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
﻿using System.Security.Claims;
using MongoDB.Bson;

namespace Rms.Risk.Mango.Services;

public class MigrationJob
{
    public enum JobType
    {
        Copy,
        Download,
        Upload
    }

    public class CollectionJob
    {
        public DateTime      StartedAtUtc          { get; set; }
        public DateTime      FinishedAtUtc         { get; set; }
        public string        SourceCollection      { get; init; } = "";
        public string        DestinationCollection { get; init; } = "";
        public BsonDocument? Filter                { get; set; }
        public BsonDocument? Projection            { get; set; }

        public long       Count     { get; set; }
        public long?      Cleared   { get; set; }
        public long       Copied    { get; set; }
        public double     Progress  => Count == 0 ? 0.0 : Copied / (double)Count;
        public bool       Complete  { get; set; }
        public Exception? Exception { get; set; }

        public string Error => Exception?.Message ?? "";

        public override string ToString()
            => $"{SourceCollection} -> {DestinationCollection} ({Count} {Cleared} {Progress:P2}) {Error}";

        public double DocsPerSecond
        {
            get
            {
                if (Copied == 0 || Elapsed == TimeSpan.Zero)
                    return 0.0;

                return Copied / Elapsed.TotalSeconds;
            }
        }

        public TimeSpan Elapsed
        {
            get
            {
                if ( StartedAtUtc == default)
                    return TimeSpan.Zero;

                var end      = Complete ? FinishedAtUtc : DateTime.UtcNow;
                var duration = end - StartedAtUtc;

                return duration;
            }
        }

        public TimeSpan Remaining
        {
            get
            {
                var dps = DocsPerSecond;
                if (dps == 0)
                    return TimeSpan.Zero;

                return TimeSpan.FromSeconds((Count - Copied) / dps);
            }
        }


        public long TotalReadMSec  { get; set;}
        public long TotalWriteMSec { get; set;}

        public double ReadDPS
        {
            get
            {
                if (Copied == 0 || TotalReadMSec == 0L )
                    return 0.0;

                return 1000.0 * Copied / TotalReadMSec;
            }
        }

        public double WriteDPS
        {
            get
            {
                if (Copied == 0 || TotalWriteMSec == 0L)
                    return 0.0;

                return 1000.0 * Copied / TotalWriteMSec;
            }
        }

    }

    public int                 JobId                       { get; init; } = ++_jobId;
    public JobType             Type                        { get; init; } = JobType.Copy;
    public string              Ticket                      { get; set; }  = "";
    public string              Email                       { get; init; } = "";
    public DateTime            StartedAtUtc                { get; set; }
    public DateTime            FinishedAtUtc               { get; set; }
    public string              SourceDatabase              { get; init; } = "";
    public string              SourceDatabaseInstance      { get; init; } = "";
    public string              DestinationDatabase         { get; init; } = "";
    public string              DestinationDatabaseInstance { get; init; } = "";
    public int                 BatchSize                   { get; init; } = 1_000;
    public bool                Upsert                      { get; init; }
    public bool                ClearDestinationBefore      { get; init; }
    public bool                DisableIndexes              { get; init; } = false;
    public List<CollectionJob> Status                      { get; init; } = [];
    public bool                Complete                    { get; set; }
    public Exception?          Exception                   { get; set; }

    private static int _jobId;

    public string Error => Exception?.Message ?? "";

    /// <summary>
    /// For Type.Upload, this is the temp file name with the zip just uploaded by user.
    /// </summary>
    public string UploadedFileName { get; init; } = "";

    /// <summary>
    /// For Type.Download, this is the result of the download operation, e.g. URL you need to access to download the file.
    /// </summary>
    public string DownloadUrl { get; set; } = "";

    /// <summary>
    /// Maximum number of parallel write operations to run for this job.
    /// </summary>
    public int MaxDegreeOfParallelism { get; set; } = 5;

    public override string ToString()
        => Type switch
        {
            JobType.Copy     => $"[{JobId:000}] Migrate {SourceDatabase} -> {DestinationDatabase} ({Status.Count}): {Email}",
            JobType.Download => $"[{JobId:000}] Download from {SourceDatabase} ({Status.Count}): {Email}",
            JobType.Upload   => $"[{JobId:000}] Upload to {SourceDatabase} ({Status.Count}): {Email}",
            _                => $"[{JobId:000}] {Type} {SourceDatabase} -> {DestinationDatabase} ({Status.Count}): {Email}"
        };
}

public interface IMigrationEngine
{
    List<MigrationJob> List();
    Task               Add(MigrationJob    job, ClaimsPrincipal user);
    Task               Cancel(MigrationJob job, ClaimsPrincipal user);
}