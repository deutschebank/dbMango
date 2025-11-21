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
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rms.Risk.Mango.Pivot.Core.MongoDb;

namespace Rms.Risk.Mango.Pivot.UI.Services;

public static class DatabaseStructureLoader
{
    public const string AuditCollection = "dbMango-Audit";

    [JsonConverter(typeof(StringEnumConverter))]
    public enum FieldSorting
    {
        Asc, Desc, Hashed,Text
    }

    public class SyncStructureOptions
    {
        public bool DryRun            { get; set; } = true;
        public bool CreateCollections { get; set; } = true;
        public bool CreateIndexes     { get; set; } = true;
        public bool RemoveCollections { get; set; } = true;
        public bool RemoveIndexes     { get; set; } = true;
    }

    public class CollectionStructure
    {
        public string               Name      { get; set; } = string.Empty;
        public bool                 IsSharded { get; set; }
        public List<IndexStructure> Indexes   { get; init; } = [];

        public override string ToString()
        {
            var indexStr = string.Join(", ", Indexes.Select(i => i.ToString()));
            return $"Collection: {Name}, {(IsSharded ? "sharded, " : "")}Indexes: [{indexStr}]";
        }
    }

    public class IndexStructure
    {
        public string Name                  { get; set; } = string.Empty;
        public List<KeyValuePair<string, FieldSorting>> Key { get; set; } = [];
        public bool Unique                  { get; set; }
        public int? ExpireAfterSeconds      { get; set; }

        public override string ToString()
        {
            var keyStr = string.Join(", ", Key.Select(kv => $"{kv.Key}: {kv.Value}"));
            return $"Index: {Name}, Key: [{keyStr}], Unique: {Unique}, ExpireAfterSeconds: {ExpireAfterSeconds}";
        }

        /// <summary>
        /// This method creating Json expected by IndexEditComponent
        /// </summary>
        public string ToJson()
        {
            var keyObj = new BsonDocument();
            foreach (var kv in Key)
            {
                object val = kv.Value switch
                {
                    FieldSorting.Asc    => 1,
                    FieldSorting.Desc   => -1,
                    FieldSorting.Hashed => "hashed",
                    FieldSorting.Text   => "text",
                    _ => 1
                };
                keyObj.Add(kv.Key, BsonValue.Create(val));
            }

            var doc = new BsonDocument
            {
                { "key", keyObj },
                { "name", Name }
            };

            if (Unique)
                doc.Add("unique", true);
            if (ExpireAfterSeconds.HasValue)
                doc.Add("expireAfterSeconds", ExpireAfterSeconds.Value);

            return doc.ToJson(new()  { Indent = true });
        }
    }

    public static async Task<IndexStructure[]> LoadIndexes( IMongoDbDatabaseAdminService admin, string coll, CancellationToken token)
    {
        var res = await admin.RunCommand(new()
        {
            { "listIndexes", coll },
            { "comment", $"Backup structure for {admin.Database}.{coll}" }
        }, token);

        var arr = res["cursor"]["firstBatch"].AsBsonArray;
        var indexes = new List<IndexStructure>();
        foreach ( var item in arr )
        {
            var doc  = item.AsBsonDocument;
            var name = doc.GetValue("name", "").AsString;
            var index = new IndexStructure
            {
                Name               = name,
                Unique             = doc.GetValue("unique", false).ToBoolean(),
                ExpireAfterSeconds = doc.Contains("expireAfterSeconds") ? doc["expireAfterSeconds"].ToInt32() : null
            };

            if (doc.TryGetValue("key", out var keyDoc) && keyDoc.IsBsonDocument)
            {
                foreach (var keyElem in keyDoc.AsBsonDocument.Elements)
                {
                    var sorting =
                        keyElem.Value.IsString && keyElem.Value.AsString == "text"
                            ? FieldSorting.Text
                            : keyElem.Value.IsString && keyElem.Value.AsString == "hashed"
                                ? FieldSorting.Hashed
                                : keyElem.Value.ToInt32() == 1
                                    ? FieldSorting.Asc
                                    : FieldSorting.Desc;
                    index.Key.Add(new(keyElem.Name, sorting));
                }

                // _id is a special case
                if ( index.Key is [{ Key: "_id", Value: FieldSorting.Asc or FieldSorting.Desc }_] )
                    index.Unique = true;
            }

            indexes.Add(index);
        }
        return indexes.ToArray();
    }

    public static async Task<List<CollectionStructure>> LoadCollections(IMongoDbDatabaseAdminService db, IMongoDbDatabaseAdminService admin, CancellationToken token)
    {
        var command = new BsonDocument
        {
            { "listCollections", 1 },
        };

        var result      = await db.RunCommand(command);
        var collections = result["cursor"]?["firstBatch"]?.AsBsonArray;

        var res = new Dictionary<string, CollectionStructure>(StringComparer.OrdinalIgnoreCase);

        foreach (var coll in (collections ?? []).OfType<BsonDocument>())
        {
            var name = coll.GetValue("name", "").AsString;
            if (name == AuditCollection || res.ContainsKey(name))
                continue; // Skip audit collection and already processed collections

            var indexes = await LoadIndexes(db, name, token);

            var collection = new CollectionStructure { Name = name };
            res[name] = collection;
            collection.Indexes.AddRange(indexes);
        }

        // Check if collections are sharded in parallel as it can be a lengthy operation
        var tasks = res.Values.Select(async c =>
        {
            c.IsSharded = await admin.IsSharded(db.Database, c.Name);
            return c;
        });

        await Task.WhenAll(tasks);

        return res.Values
            .OrderBy(x => x.Name)
            .ToList()
            ;
    }

    public enum DiffType
    {
        Add, Remove, Modify
    }

    public class CollectionStructureDiff : CollectionStructure
    {
        public DiffType Type { get; set; } = DiffType.Add;
    }

    public class IndexStructureDiff : IndexStructure
    {
        public DiffType Type { get; set; } = DiffType.Add;
    }



    public class StructureDifference
    {
        public List<CollectionStructureDiff> ToRemove      { get; } = [];
        public List<CollectionStructureDiff> ToAdd         { get; } = [];
        public List<string>                  ToBeSharded   { get; } = [];
        public List<string>                  ToBeUnSharded { get; } = [];
    }

    public static StructureDifference GetStructureDifference(List<CollectionStructure> currentCollections, List<CollectionStructure> targetCollections)
    {
        var difference = new StructureDifference();

        // Find collections to remove
        foreach (var current in currentCollections
            .Where(current => targetCollections.All(t => t.Name != current.Name))
            )
        {
            // Collection does not exist in target, add to removal list
            difference.ToRemove.Add(new () { Name = current.Name, Type = DiffType.Remove });
        }

        // Find collections to add/modify
        foreach (var target in targetCollections)
        {
            if (currentCollections.All(c => c.Name != target.Name))
            {
                // Collection does not exist in current, add to addition list
                var newTarget = new CollectionStructureDiff
                {
                    Name    = target.Name,
                    Type    = DiffType.Add,
                    Indexes = target.Indexes.Select(x => new IndexStructureDiff
                    {
                        Name               = x.Name,
                        Key                = x.Key.ToList(),
                        Unique             = x.Unique,
                        ExpireAfterSeconds = x.ExpireAfterSeconds,
                        Type               = DiffType.Add
                    }).ToList<IndexStructure>()
                };
                difference.ToAdd.Add(newTarget);
            }
            else
            {
                // Check indexes for existing collections
                var current         = currentCollections.First(c => c.Name == target.Name);
                var indexesToRemove = GetIndexesToRemove(current, target);
                var indexesToAdd    = GetIndexesToAdd(current, target);

                // Add indexes to remove
                if (indexesToRemove.Count > 0)
                {
                    difference.ToRemove.Add(new()
                    {
                        Name    = current.Name,
                        Type    = DiffType.Modify,
                        Indexes = indexesToRemove.Select(x => new IndexStructureDiff
                        {
                            Name               = x.Name,
                            Key                = x.Key.ToList(),
                            Unique             = x.Unique,
                            ExpireAfterSeconds = x.ExpireAfterSeconds,
                            Type               = DiffType.Remove
                        }).ToList<IndexStructure>()
                    });
                }

                // Add indexes to add
                if (indexesToAdd.Count > 0)
                {
                    difference.ToAdd.Add(new()
                    {
                        Name    = target.Name,
                        Type    = DiffType.Modify,
                        Indexes = indexesToAdd.Select(x => new IndexStructureDiff
                        {
                            Name               = x.Name,
                            Key                = x.Key.ToList(),
                            Unique             = x.Unique,
                            ExpireAfterSeconds = x.ExpireAfterSeconds,
                            Type               = DiffType.Add
                        }).ToList<IndexStructure>()
                    });
                }
            }
        }
        

        foreach (var (target, source) in targetCollections
            .Select(target => (Target : target, Source: currentCollections.FirstOrDefault(c => c.Name == target.Name)))
            .Where(x => x.Source != null && x.Source.IsSharded != x.Target.IsSharded ) 
            )
        {
            if ( source!.IsSharded )
                difference.ToBeUnSharded.Add( target.Name );
            else
                difference.ToBeSharded.Add( target.Name );
        }

        return difference;
    }


    private static List<IndexStructureDiff> GetIndexesToRemove(CollectionStructure current, CollectionStructure target)
    {
        var toRemove = new List<IndexStructureDiff>();
        var targetIndexes = target.Indexes.ToDictionary(i => i.Name);
        // Iterate through current indexes
        foreach (var currentIndex in current.Indexes)
        {
            // Check if the index exists in the target collection
            if (!targetIndexes.ContainsKey(currentIndex.Name))
            {
                // Index does not exist in target, add to removal list
                toRemove.Add(new()
                {
                    Name               = currentIndex.Name,
                    Key                = currentIndex.Key.ToList(),
                    Unique             = currentIndex.Unique,
                    ExpireAfterSeconds = currentIndex.ExpireAfterSeconds,
                    Type               = DiffType.Remove
                });
            }
        }
        return toRemove;
    }

    private static List<IndexStructureDiff> GetIndexesToAdd(CollectionStructure current, CollectionStructure target)
    {
        var indexesToAdd = new List<IndexStructureDiff>();

        // Create a dictionary of current indexes for quick lookup
        var currentIndexes = current.Indexes.ToDictionary(i => i.Name);

        // Iterate through target indexes
        foreach (var targetIndex in target.Indexes)
        {
            // Check if the index exists in the current collection
            if (!currentIndexes.TryGetValue(targetIndex.Name, out var currentIndex))
            {
                // Index does not exist, add it to the list
                indexesToAdd.Add(new()
                {
                    Name               = targetIndex.Name,
                    Key                = targetIndex.Key.ToList(),
                    Unique             = targetIndex.Unique,
                    ExpireAfterSeconds = targetIndex.ExpireAfterSeconds,
                    Type               = DiffType.Add
                });
                continue;
            }

            // Compare index properties to determine if it needs to be added
            if (!AreIndexesEquivalent(currentIndex, targetIndex))
            {
                // Index exists but is different, add it to the list
                indexesToAdd.Add(new()
                {
                    Name               = targetIndex.Name,
                    Key                = targetIndex.Key.ToList(),
                    Unique             = targetIndex.Unique,
                    ExpireAfterSeconds = targetIndex.ExpireAfterSeconds,
                    Type               = DiffType.Modify // or DiffType.Add based on your logic
                });
            }
        }

        return indexesToAdd;
    }

    private static bool AreIndexesEquivalent(IndexStructure current, IndexStructure target)
    {
        // Compare keys
        if (current.Key.Count != target.Key.Count ||
            !current.Key.SequenceEqual(target.Key))
        {
            return false;
        }

        // Compare other properties
        return current.Unique == target.Unique &&
               current.ExpireAfterSeconds == target.ExpireAfterSeconds;
    }

    public static List<CollectionStructure> ParseCollections(string json)
    {
        return JsonUtils.FromJson<List<CollectionStructure>>(json) ?? new List<CollectionStructure>();
    }

    public static async Task<List<(bool Success, string Message)>> SyncStructure(IMongoDbDatabaseAdminService admin, StructureDifference difference, SyncStructureOptions? options = null)
    {
        options ??= new ()
        {
            DryRun            = false,
            CreateCollections = true,
            CreateIndexes     = true,
            RemoveCollections = true,
            RemoveIndexes     = true
        };

        var progress = new List<(bool Success, string Message)>();
        // Create missing collections
        foreach (var collection in difference.ToAdd.OfType<CollectionStructureDiff>())
        {
            try
            {
                if ( collection.Type == DiffType.Add )
                {
                    if ( options.CreateCollections )
                    {
                        if ( !options.DryRun )
                        {
                            var createCollectionCommand = new BsonDocument
                            {
                                { "create", collection.Name }
                            };

                            await admin.RunCommand(createCollectionCommand);
                        }
                        progress.Add((true, $"{collection.Name}: Collection created"));
                    }
                }

                foreach (var index in collection.Indexes.OfType<IndexStructureDiff>())
                {
                    try
                    {
                        if (!options.CreateIndexes) 
                            continue;

                        if ( !options.DryRun)
                        {
                            await CreateIndexes(admin, collection.Name, [index]);
                        }
                        progress.Add((true, $"{collection.Name}: Index {(index.Type == DiffType.Add ? "created":"updated")} '{index.Name}'"));
                    }
                    catch (Exception e)
                    {
                        progress.Add((false, $"{collection.Name}: Failed to {(index.Type == DiffType.Add ? "create":"update")} index '{index.Name}': {e.Message}"));
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                progress.Add((false, $"{collection.Name}: Failed to create collection: {ex.Message}"));
                continue;
            }
        }

        // Remove indexes and collections marked as ToRemove
        foreach (var collection in difference.ToRemove)
        {
            try
            {
                if ( collection.Type != DiffType.Remove)
                {
                    // just delete specified indexes
                    foreach (var index in collection.Indexes)
                    {
                        try
                        {
                            if (!options.RemoveIndexes) 
                                continue;

                            if ( !options.DryRun )
                            {
                                var dropIndexCommand = new BsonDocument
                                {
                                    { "dropIndexes", collection.Name },
                                    { "indexes", index.Name }
                                };

                                await admin.RunCommand(dropIndexCommand);
                            }
                            progress.Add((true, $"Index '{index.Name}' on collection '{collection.Name}' removed."));
                        }
                        catch (Exception e)
                        {
                            progress.Add((false, $"Failed to remove index '{index.Name}' on collection '{collection.Name}': {e.Message}"));
                            continue;
                        }
                    }
                }
                else
                {
                    // delete the whole collection
                    if (options.RemoveCollections)
                    {
                        if (!options.DryRun)
                        {
                            var dropCollectionCommand = new BsonDocument
                            {
                                { "drop", collection.Name }
                            };

                            await admin.RunCommand(dropCollectionCommand);
                        }
                        progress.Add((true, $"Collection '{collection.Name}' and its indexes removed."));
                    }
                }
            }
            catch (Exception ex2)
            {
                progress.Add((false, $"Failed to {(collection.Type == DiffType.Remove ? "remove" : "remove indexes for" )} collection '{collection.Name}': {ex2.Message}"));
                continue;
            }
        }

        return progress;
    }

    public static async Task CreateIndexes(
        IMongoDbDatabaseAdminService admin, 
        string      collection,
        IndexStructure[]             indexes,
        CancellationToken            token = default
        )
    {
        var createIndexCommand = new BsonDocument
        {
            { "createIndexes", collection },
            { "indexes", new BsonArray
                (
                    indexes.Select( index =>
                        // Create the index document
                        {
                            var d = new BsonDocument
                            {
                                { "key", new BsonDocument(
                                    index.Key
                                       .Select(kvp => new BsonElement(kvp.Key, kvp.Value switch
                                        {
                                            FieldSorting.Asc    => 1,
                                            FieldSorting.Desc   => -1,
                                            FieldSorting.Hashed => "hashed",
                                            FieldSorting.Text   => "text",
                                            _                   => throw new InvalidOperationException($"Unknown field sorting: {kvp.Value}")
                                        }))) },
                                { "name", index.Name },
                                { "unique", index.Unique }
                            };

                            if ( index.ExpireAfterSeconds.HasValue )
                                d["expireAfterSeconds"] = index.ExpireAfterSeconds.Value;
                            return d;
                        }
                    )
                )
            }
        };


        await admin.RunCommand(createIndexCommand, token);
    }
}