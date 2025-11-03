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
using MongoDB.Bson.Serialization;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class CollStatsModel
{
    public bool                              Sharded      { get; set; }
    public bool                              Capped       { get; set; }
    public CollStatsData                     WiredTiger   { get; set; } = new CollStatsData();
    public Dictionary<string, CollStatsData> IndexDetails { get; set; } = new();

    // New fields added based on the provided JSON structure
    public string Ns                           { get; set; } = string.Empty;
    public long Count                          { get; set; }
    public long Size                           { get; set; }
    public long StorageSize                    { get; set; }
    public long TotalIndexSize                 { get; set; }
    public long TotalSize                      { get; set; }
    public Dictionary<string, long> IndexSizes { get; set; } = new();
    public double AvgObjSize                   { get; set; }
    public long MaxSize                        { get; set; }
    public int NIndexes                        { get; set; }
    public int ScaleFactor                     { get; set; }
    public int NChunks                         { get; set; }

    public static CollStatsModel FromJson(string json)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                AllowTrailingCommas         = true
            };
            return System.Text.Json.JsonSerializer.Deserialize<CollStatsModel>(json, options) ?? new();
        }
        catch
        {
            // Log the exception if necessary
            return new ();
        }
    }
}

public class CollStatsData
{
    public Metadata Metadata                         { get; set; } = new();
    public string CreationString                     { get; set; } = "";
    public string Type                               { get; set; } = "";
    public string Uri                                { get; set; } = "";
    public Dictionary<string, ulong>? LSM            { get; set; }
    public Dictionary<string, ulong>? Autocommit     { get; set; }
    public Dictionary<string, ulong>? Backup         { get; set; }
    public Dictionary<string, ulong>? BlockManager   { get; set; }
    public Dictionary<string, ulong>? Btree          { get; set; }
    public Dictionary<string, ulong>? Cache          { get; set; }
    public Dictionary<string, ulong>? CacheWalk      { get; set; }
    public Dictionary<string, ulong>? Checkpoint     { get; set; }
    public Dictionary<string, ulong>? Compression    { get; set; }
    public Dictionary<string, ulong>? Cursor         { get; set; }
    public Dictionary<string, ulong>? Reconciliation { get; set; }
    public Dictionary<string, ulong>? Session        { get; set; }
    public Dictionary<string, ulong>? Transaction    { get; set; }
}

public class Metadata
{
    public int FormatVersion { get; set; }
}