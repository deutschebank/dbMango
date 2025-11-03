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
using log4net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Rms.Risk.Mango.Pivot.Core.Models;

/// <summary>
/// Implementation of IPivotTableDataSource that goes direct to the mongo database
/// </summary>
public class MongoDbDataSource : IPivotTableDataSource, IPivotTableDataSourceMetaProvider
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    // ReSharper disable InconsistentNaming
    public static int      PivotCacheTTLHours   = 1;
    public static TimeSpan MetaCacheTTL         = TimeSpan.FromMinutes(30);
    public static int      PivotMaxReturnedRows = 500_000;
    public static string   SourcePrefix         = "Forge";
    public static string   CacheCollectionName  = "dbMango-Pivot-Cache";
    // ReSharper restore InconsistentNaming

    private const int    MaxCachedRows       = 25000;

    private readonly MongoDbConfigRecord     _config;
    private readonly MongoDbSettings         _settings;
    private readonly string                  _databaseInstance;
    private          IMongoDatabase?         _database;
    private readonly Lock                    _syncObject = new();
    private readonly List<GroupedCollection> _allMeta    = [];
    private          DateTime                _allMetaValidUntil = DateTime.MinValue;


    public string User { get; set; } = "";

    readonly SemaphoreSlim _threadLock = new(1);

    internal IMongoDatabase Database
    {
        get
        {
            if (_database != null)
                return _database;

            lock (_syncObject)
            {
                Thread.MemoryBarrier();

                if (_database != null)
                    return _database;

                return _database = MongoDbHelper.GetDatabase(_config, _settings, _databaseInstance);
            }
        }
    }

    public string SourceId => _config!.GetKey(_databaseInstance);
    public string Prefix => SourcePrefix;
    public override string ToString() => SourceId;

    private readonly ConcurrentDictionary<string, FieldMapping> _fieldMap  = new();

    public MongoDbDataSource(MongoDbConfigRecord config, MongoDbSettings settings, string? databaseInstance)
    {
        _config                = config;
        _settings              = settings;
        _databaseInstance      = databaseInstance ?? config.MongoDbDatabase;
        _config.Check();
    }

    private async Task InitAsync(string collectionName, bool skipCache, CancellationToken token = default )
    {
        //multiple callers call this method, so ensure only 1 at a time passes through this logic
        await _threadLock.WaitAsync(token);

        try
        {
            // Force test connection, and reset _database if disconnected
            if (!MongoDbHelper.IsConnected(_database, true))
                _database = null;

            if (_fieldMap.ContainsKey(collectionName) && !skipCache)
                return;

            var sw = Stopwatch.StartNew();

            _log.Debug($"Loading field map for Collection=\"{collectionName}\" MongoDb=\"{_config?.MongoDbDatabase}\" URL=\"{_config?.MongoDbUrl}\"");

            if (await LoadCachedFieldMappingAsync(collectionName, skipCache, token))
            {
                sw.Stop();
                _log.Info($"Loaded cached field map for Collection=\"{collectionName}\" FieldDefs={_fieldMap[collectionName].Count} CalcFields={_fieldMap[collectionName].CalculatedFields.Count()} Lookups={_fieldMap[collectionName].Data.Lookups.Count} MongoDb=\"{_config?.MongoDbDatabase}\" URL=\"{_config?.MongoDbUrl}\"" +
                          $"\tElapsed=\"{sw.Elapsed}\""
                );
                return;
            }

            //no cached field mappings found, so create it and store it in the db
            var swUpdateFieldMappings = Stopwatch.StartNew();
            await UpdateFieldMappings(collectionName, skipCache, token);
            swUpdateFieldMappings.Stop();

            var swUpdateLookups = Stopwatch.StartNew();
            var lookups         = await UpdateLookups(collectionName, token);
            swUpdateLookups.Stop();

            var swUpdateCalculatedFields = Stopwatch.StartNew();
            await UpdateCalculatedFields(collectionName, token);
            swUpdateCalculatedFields.Stop();

            await CacheFieldMappingAsync(collectionName, token);

            sw.Stop();
            _log.Info($"Created field map for Collection=\"{collectionName}\", FieldDefs={_fieldMap[collectionName].Count}, CalcFields={_fieldMap[collectionName].CalculatedFields.Count()}, Lookups={lookups}, MongoDb=\"{_config?.MongoDbDatabase}\", URL=\"{_config?.MongoDbUrl}\",\n" +
                      $"ElapsedMs=\"{sw.Elapsed.TotalMilliseconds}\",\n" +
                      $"UpdateFieldMappingsMs=\"{swUpdateFieldMappings.Elapsed.TotalMilliseconds}\",\n" +
                      $"UpdateLookupsMs=\"{swUpdateLookups.Elapsed.TotalMilliseconds}\",\n" +
                      $"UpdateCalculatedFieldsMs=\"{swUpdateCalculatedFields.Elapsed.TotalMilliseconds}\"\n"
            );
        }
        finally
        {
            _threadLock.Release();
        }
    }

    public async Task<List<GroupedCollection>> GetAllMeta(bool force = false, CancellationToken token = default)
    {
        if ( force )
        {
            _allMeta.Clear();
            PivotMetaCache.Clear();
        }

        if (DateTime.Now > _allMetaValidUntil )
            _allMeta.Clear();

        if ( _allMeta.Count > 0 )
            return _allMeta;

        await this.PreloadCollections(_allMeta, User, token);
        _allMetaValidUntil = DateTime.Now + MetaCacheTTL;

        return _allMeta;
    }


    private async Task<bool> LoadCachedFieldMappingAsync(string collectionName, bool skipCache, CancellationToken token = default )
    {
        if ( skipCache )
            return false;

        var coll   = GetCollectionWithRetries<FieldMappingData>(collectionName+"-Meta");
        var cursor = await coll.FindAsync("{ _id : \"CachedFieldMapping\"}", cancellationToken: token);

        while ( await cursor.MoveNextAsync(token) )
        {
            var doc = cursor.Current.FirstOrDefault();
            if ( doc == null )
                return false;

            if ( doc.CachedAt.Date != DateTime.UtcNow.Date ) // cache is too old
                return false;

            doc.PostLoad();
            _fieldMap[collectionName] = new( doc.UseMapping ) { Data = doc };
            return true;
        }
        return false;
    }

    public async Task CacheFieldMappingAsync(string collectionName, CancellationToken token = default)
    {
        var doc = _fieldMap[collectionName].Data.Clone();
        doc.PreSave();

        var coll = GetCollectionWithRetries<FieldMappingData>(collectionName + "-Meta");
        await coll.ReplaceOneAsync( "{_id : \"CachedFieldMapping\"}", doc, new ReplaceOptions {IsUpsert = true}, token);
    }

    private async Task<int> UpdateLookups(string collectionName, CancellationToken token = default)
    {
        var coll = GetCollectionWithRetries<BsonDocument>($"{collectionName }-Meta");
        var doc = (await coll.FindAsync(new BsonDocument { { "_id", "Lookups" } }, cancellationToken: token)).FirstOrDefault();

        if (doc == null)
            return 0;

        var count = 0;
        foreach (var elem in doc.Elements.Where(x => !x.Name.StartsWith("_")).Select(x => x.Name))
        {
            try
            {
                var stages = doc[elem]
                            .ToJson()
                            .Replace( "@", "$" )
                            .Replace( "#", "." )
                            .TrimStart(' ', '\t', '\r', '\n', '[')
                            .TrimEnd(  ' ', '\t', '\r', '\n', ']')
                    ;

                _fieldMap[collectionName].AddLookup( elem, stages );
                count += 1;
            }
            catch
            {
                // ignore
            }
        }
        return count;
    }

    private async Task UpdateCalculatedFields(string collectionName, CancellationToken token = default)
    {
        var coll = GetCollectionWithRetries<BsonDocument>($"{collectionName}-Meta");
        var doc = (await coll.FindAsync(new BsonDocument { { "_id", "CalculatedFields" } }, cancellationToken: token)).FirstOrDefault();

        if (doc == null)
            return;

        foreach (var elem in doc.Elements.Where(x => !x.Name.StartsWith("_")).Select(x => x.Name))
        {
            try
            {
                var e = doc[elem].AsBsonDocument;

                var formula = e.GetValue("Formula", null)?.ToJson()
                               .Replace("@", "$")
                    ;

                if (string.IsNullOrWhiteSpace(formula))
                    continue;

                var drill = e.GetValue("DrillDown", null)?.ToJson()
                             .Replace("@", "$")
                             .Replace("#", ".")
                     ?? ""
                    ;

                var lookupDef = e.GetValue("LookupDef", null)?.AsBsonArray
                    ;

                var aggregationOperator = e.GetValue("AggregationOperator", null)?.AsString ?? "$sum";

                _fieldMap[collectionName].AddCalculatedField(elem, formula, drill, lookupDef?.Values.Select( x => x.AsString ).ToArray(), aggregationOperator);
            }
            catch
            {
                // ignore
            }
        }
    }

    private async Task UpdateFieldMappings(string collectionName, bool skipCache, CancellationToken token = default )
    {
        var coll = GetCollectionWithRetries<BsonDocument>($"{collectionName}-Meta");
        var doc = (await coll.FindAsync(new BsonDocument { { "_id", "FieldsMap" } }, cancellationToken: token)).FirstOrDefault();

        if (doc == null)
        {
            _fieldMap[collectionName] = new(false);
            return;
        }

        var useIds = doc.Contains("_UseIds") ? doc["_UseIds"] : null;

        var res = new FieldMapping(useIds?.AsBoolean ?? false);
        foreach (var elem in doc.Elements.Where(x => !x.Name.StartsWith("_")).Select(x => x.Name))
        {
            var d = doc[elem].AsBsonDocument;

            res[elem] = new()
            {
                Id      = d.Names.Any(x => x == "Id") ? d["Id"].ToInt32() : 0,
                Type    = ReinterpretType( d["Type"].ToString()! ),
                Purpose = Convert(d["Purpose"].ToString()!)
            };
        }

        _fieldMap[collectionName] = res;

        // add fields missing in the mapping but present in the data set

        await UpdateMissingFieldMappings(collectionName, skipCache, token);
    }

    private static PivotFieldPurpose Convert( string s )
    {
        switch (s.ToLower())
        {
            case "key":
                return PivotFieldPurpose.Key;
            case "primary key 1":
                return PivotFieldPurpose.PrimaryKey1;
            case "primary key 2":
                return PivotFieldPurpose.PrimaryKey2;
            case "info":
                return PivotFieldPurpose.Info;
            case "hidden":
                return PivotFieldPurpose.Hidden;
            default:
                return PivotFieldPurpose.Data;
        }
    }

    private async Task UpdateMissingFieldMappings(string collectionName, bool skipCache, CancellationToken token = default )
    {
        if ( _fieldMap[collectionName].UseMapping ) // can't pick up missing fields
            return;

        // collection does not use field ids. pick up missing fields

        var coll = GetCollectionWithRetries<BsonDocument>(collectionName);

        var cobs   = await GetCobDatesAsync(collectionName, skipCache, token);
        var filter = "";

        if ( cobs.Length > 0 )
            filter = $"{{ $match : {{ \"COB\" : ISODate(\"{cobs[^1]}\") }} }}, ";

        var json = "["+filter+"{ $sample: { size: 16 } }]";
        var pipeline = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(json).Select(p => p.AsBsonDocument).ToList();

        try
        {
            var options = new AggregateOptions
            {
                BypassDocumentValidation = true,
                //AllowDiskUse = true
            };

            using var cursor = await coll.AggregateAsync<BsonDocument>( pipeline, options, token);
            while ( await cursor.MoveNextAsync(token) )
            {
                var batch = cursor.Current;
                foreach ( var doc in batch )
                {
                    UpdateMissingFieldMappings(collectionName, doc );
                }
            }
        }
        catch ( Exception )
        {
            // ignore
        }
    }

    public void UpdateMissingFieldMappings(
        string                                     collectionName, 
        BsonDocument                               doc, 
        string                                     prefix = "")
    {
        foreach ( var element in doc.Elements )
        {
            if ( element.Name == "_id" )
                continue;


            var name = prefix == ""
                    ? element.Name
                    : $"{prefix}.{element.Name}"
                ;

            if ( !_fieldMap.ContainsKey(collectionName))
                _fieldMap[collectionName] = new(false);

            if (_fieldMap[collectionName].ContainsKey(name) )
                continue;

            var shouldIgnore = _fieldMap[collectionName].Fields
                                                       .Any( x => x.Value.Purpose == PivotFieldPurpose.Hidden && name.IndexOf( x.Key + ".", StringComparison.Ordinal ) == 0 );

            if ( shouldIgnore )
                continue;

            var  val = element.Value;
            Type t;
            switch ( val.BsonType )
            {
                case BsonType.Double    : t = typeof(double);   break;
                case BsonType.String    : t = typeof(string);   break;
                case BsonType.Document  : UpdateMissingFieldMappings(collectionName, val.AsBsonDocument, name ); continue;
                case BsonType.Boolean   : t = typeof(bool);     break;
                case BsonType.DateTime  : t = typeof(DateTime); break;
                case BsonType.Null      : continue; // this can be an object
                case BsonType.Symbol    : t = typeof(string);   break;
                case BsonType.Int32     : t = typeof(int);      break;
                case BsonType.Timestamp : t = typeof(DateTime); break;
                case BsonType.Int64     : t = typeof(long);     break;
                default                 : continue;
            }

            UpdateMappingIfFieldIsMissing( collectionName, name, t);
        }
    }

    public void UpdateMappingIfFieldIsMissing(
        string                                     collectionName,
        string                                     name,
        Type                                       t
        )
    {
        if ( !_fieldMap.ContainsKey(collectionName))
            _fieldMap[collectionName] = new(false);

        if (_fieldMap[collectionName].ContainsKey(name) ) return;

        var shouldIgnore = _fieldMap[collectionName].Fields
                                                   .Any( x => x.Value.Purpose == PivotFieldPurpose.Hidden && name.IndexOf( x.Key + ".", StringComparison.Ordinal ) == 0 );

        if (shouldIgnore) return;

        var isNumber =   t == typeof(double)
                      || t == typeof(decimal)
                      || t == typeof(int)
                      || t == typeof(long)
            ;

        _fieldMap[collectionName][name] = new()
        {
            Id = 0,
            Purpose = isNumber
                ? PivotFieldPurpose.Data
                : PivotFieldPurpose.Info,
            Type = t
        };
    }


    private static Type ReinterpretType( string type )
    {
        var t = Type.GetType( type );
        if ( t != null )
            return t;

        switch ( type )
        {
            case "double":
                return typeof(double);
            case "string":
                return typeof(string);
            case "int32":
                return typeof(int);
            case "int64":
                return typeof(long);
            case "decimal":
                return typeof(decimal);
            case "date":
                return typeof(DateTime);
            default:
                return typeof(double);
        }
    }

    ///<summary>
    /// CollectionType.All: If includeMeta is CollectionType.All, it simply returns all collection names as an array of strings.
    ///    The OfType_string_() ensures that only strings are included (though they should all be strings already).
    ///
    /// CollectionType.NoMeta: If includeMeta is CollectionType.NoMeta, it filters the collection names to include only those
    ///    that do not have a corresponding metadata collection (i.e., a collection with the same name plus "-Meta"). 
    ///
    /// CollectionType.MetaOnly: If includeMeta is CollectionType.MetaOnly, it filters the collection names to include only those
    ///     that do have a corresponding metadata collection.
    ///</summary>
    public async Task<string[]> GetCollectionsAsync(CollectionType includeMeta = CollectionType.All, CancellationToken token = default)
    {
        var cursor = await Database.ListCollectionsAsync(cancellationToken: token);
        var list   =  await cursor.ToListAsync(cancellationToken: token);
        var coll   = list.Select( x => x["name"].ToString() ).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);

        var res = includeMeta switch
        {
            CollectionType.All      => coll.OrderBy(x => x).ToArray(),
            CollectionType.NoMeta   => coll.Where(x => IsNotMeta(x) && !HaveMeta(coll,x)).OrderBy(x => x).ToArray(),
            CollectionType.HaveMeta => coll.Where(x => IsNotMeta(x) &&  HaveMeta(coll,x)).OrderBy(x => x).ToArray(),
            _                       => throw new ArgumentOutOfRangeException(nameof(includeMeta), includeMeta, null)
        };

        return res;

        static bool IsNotMeta(string? name) =>
            !string.IsNullOrWhiteSpace(name)
         && !name.EndsWith("-Meta", StringComparison.OrdinalIgnoreCase);

        static bool HaveMeta(HashSet<string> allCollections, string? collectionName) =>
            allCollections.Contains(collectionName + "-Meta");
    }

    public async Task<string[]> GetKeyFieldsAsync(string collectionName, CancellationToken token = default)
    {
        var fields = await GetFieldMapping(collectionName, token);

        if ( fields?.Count > 0 )
            return fields.Fields
                .Where( x => !IsData( x.Value ) )
                .OrderBy( x => x.Value.Id )
                .Select( x => x.Key )
                .ToArray()
                ;

        return new[]
        {
            "Department", "Location", "Book", "Currency", "TradeType", "SystemId", "Ver", "Expiry Date", "COB"
        }
        .OrderBy( x => x )
        .ToArray();
    }

    public FieldMapping GetFieldMapping(string collectionName) =>
        !_fieldMap.TryGetValue(collectionName, out var fields) 
            ? new(false) 
            : fields;

    public async Task<FieldMapping> GetFieldMapping(string collectionName, CancellationToken token)
    {
        if ( !_fieldMap.TryGetValue(collectionName, out var fields))
        {
            await InitAsync(collectionName, false, token);
            _fieldMap.TryGetValue(collectionName, out fields);
        }

        return fields ?? throw new ApplicationException($"Mapping for Collection=\"{collectionName}\" is not found");
    }

    private static bool IsData( SingleFieldMapping x )
    {
        if (   x.Purpose == PivotFieldPurpose.Key
         || x.Purpose == PivotFieldPurpose.PrimaryKey1
         || x.Purpose == PivotFieldPurpose.PrimaryKey2
         || x.Purpose == PivotFieldPurpose.Info
           )
        {
            return false;
        }

        return 
            x.Type == typeof(double) 
         || x.Type == typeof(decimal) 
         || x.Type == typeof(int) 
         || x.Type == typeof(long)
            ;
    }

    public async Task<string[]> GetDrilldownKeyFieldsAsync(string collectionName,PivotFieldPurpose keyLevel, CancellationToken token = default)
    {
        var fields = await GetFieldMapping(collectionName, token);
        if ( fields.Count > 0 )
            return fields.Fields
                .Where( x => x.Value.Purpose == keyLevel )
                .OrderBy( x => x.Value.Id )
                .Select( x => x.Key )
                .ToArray();

        return new[]
        {
            "SystemId", "Ver"
        }
        .OrderBy( x => x )
        .ToArray();
    }

    public async Task<string> GetDrilldownAsync(string collectionName, string name, string nullValue = "\"\"", bool equals = false, CancellationToken token = default)
    {
        var fields = await GetFieldMapping(collectionName, token);
        return fields.GetDrilldown(name, nullValue, equals );
    }

    public async Task<string[]> GetDataFieldsAsync(string collectionName, CancellationToken token = default)
    {
        var fields = await GetFieldMapping(collectionName, token);

        if ( fields == null )
            return [];

        var fieldNames       = fields.FieldNames;
        var calculatedFields = fields.CalculatedFields;
        var all              = fieldNames.Concat(calculatedFields).ToList();
        all.Sort();
        var keys = new HashSet<string>( await GetKeyFieldsAsync(collectionName, token) );
        return all.Where( x => x != "_id" && x != "id" && !keys.Contains( x ) ).ToArray();
    }

    private async Task<string[]> InternalGetCobDatesAsync(string collectionName, CancellationToken token = default)
    {
        var coll        = GetCollectionWithRetries<BsonDocument>(collectionName);
        var emptyFilter = new FilterDefinitionBuilder<BsonDocument>().Empty;

        // using DateTime? in case we have a document without COB
        // in this case Null can't be converted to DateTime and MongoDB driver throws an exception
        var fields = await GetFieldMapping(collectionName, token);
        var docs = (await coll.DistinctAsync<DateTime?>( fields.MapField( "COB" ), emptyFilter, cancellationToken: token)).ToList();

        var cobs = docs.Where(x => x != null).Select( x => x!.Value.ToString( "yyyy-MM-dd" ) ).ToArray();

        return cobs;
    }

    public async Task<string[]> GetCobDatesAsync(string collectionName, bool force = false, CancellationToken token = default)
    {
        var cobs = force 
                ? [] 
                : await this.LoadCachedCobDatesAsync(collectionName, token)
            ;

        // contains today or yesterday or last Friday if today is Monday
        if ( 
            cobs.Contains( DateTime.Now.ToString( "yyyy-MM-dd" ) )
         || cobs.Contains( (DateTime.Now - TimeSpan.FromDays( 1 )).ToString( "yyyy-MM-dd" ) )
         || (DateTime.Now.DayOfWeek == DayOfWeek.Monday &&
                cobs.Contains( (DateTime.Now - TimeSpan.FromDays( 3 )).ToString( "yyyy-MM-dd" ) ))
            )
        {
            return cobs;
        }

        cobs = await InternalGetCobDatesAsync(collectionName, token);
        await this.CacheCobDates(collectionName, cobs, token);
            
        return cobs;
    }

    public async Task<string[]> GetDepartmentsAsync(string collectionName, CancellationToken token = default)
    {
        var cob = await GetCobDatesAsync(collectionName, token: token);
        if ( cob.Length == 0 )
            return [];

        var (stillValid,cached) = await this.LoadCachedDepartmentsAsync(collectionName, token);

        if (stillValid && cached.Length > 0)
            return cached;

        var coll = GetCollectionWithRetries<BsonDocument>(collectionName);

        var fields = await GetFieldMapping(collectionName, token);

        var filter = new FilterDefinitionBuilder<BsonDocument>().Eq(fields.MapField("COB"), DateTime.ParseExact(cob[^1], "yyyy-MM-dd", null, DateTimeStyles.AssumeUniversal));
        var docs = await (await coll.DistinctAsync<string>(fields.MapField("Department"), filter, cancellationToken: token)).ToListAsync(cancellationToken: token);
        var departments = docs.Concat(cached).Distinct().OrderBy(x => x).ToArray();

        await this.CacheDepartments(collectionName, departments, token);

        return departments;
    }

    public async Task<(string, string)[]> GetDesksWithDepartmentAsync(string collectionName, CancellationToken token = default)
    {
        var (stillValid,cached) = await this.LoadCachedDesksAsync(collectionName, token);

        if (stillValid)
            return cached;

        var res = new List<(string, string)>();
        var coll = GetCollectionWithRetries<BsonDocument>(collectionName);

        try
        {
            var json = "[{ \"$group\": { \"_id\": { Department: \"$Department\", Desk: \"$Desk\" } } }]";
            var pipeline = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(json)
                                  .Select(p => p.AsBsonDocument).ToList();

            var options = new AggregateOptions
            {
                BypassDocumentValidation = true,
                //AllowDiskUse = true
            };

            using var cursor = await coll.AggregateAsync<BsonDocument>(pipeline, options, token);

            while ( await cursor.MoveNextAsync(token) )
            {
                var batch = cursor.Current;
                foreach ( var doc in batch )
                {
                    foreach ( var element in doc.Elements )
                    {
                        var bsonValue = element.Value.AsBsonDocument;

                        var desk       = bsonValue.GetValue("Desk",       null)?.ToString();
                        var department = bsonValue.GetValue("Department", null)?.ToString();

                        if (!string.IsNullOrEmpty(desk) && !string.IsNullOrEmpty(department))
                            res.Add((desk, department));
                    }
                }
            }
        }
        catch
        {
            //do nothing
        }

        var desks = res.ToArray();
        await this.CacheDesks(collectionName, desks, token);

        return desks;
    }


    public async Task<IPivotedData> PivotAsync(
        string collectionName, 
        PivotDefinition def, 
        FilterExpressionTree.ExpressionGroup? extraFilter, 
        bool skipCache, 
        string? userName, 
        int maxFetchSize = -1,
        CancellationToken token = default )
    {
        var sw = new Stopwatch();
        sw.Start();

        ArrayBasedPivotData? data;

        // preload field mapping. we'll need it in GetQueryText
        _ = await GetFieldMapping(collectionName, token);

        if ( !skipCache )
        {
            data = await GetCachedResultAsync(collectionName, def, extraFilter, GetQueryText, token);
            if ( data != null )
            {
                sw.Stop();
                _log.Info( $"Loaded from cache. User=\"{userName}\" Pivot=\"{def.Name}\" PivotType={def.PivotType} Collection=\"{collectionName}\" ExtraFilter=\"{extraFilter}\" executed in Elapsed=\"{sw.Elapsed}\"" );
                return data.Count == 0 
                        ? ArrayBasedPivotData.NoData 
                        : data
                    ;
            }
        }

        try
        {
            data = await AggregationPivotAsync(collectionName, def, extraFilter, maxFetchSize, token);

            if ( data is { Count: > 0, Headers.Count: > 0 } )
                await CacheResultsAsync(collectionName, data, def, extraFilter, GetQueryText, token);

            return data.Count == 0 
                    ? ArrayBasedPivotData.NoData 
                    : data
                ;
        }
        finally
        {
            sw.Stop();
#pragma warning disable CS8604 // Possible null reference argument.
            _log.Info( $"Executed. User=\"{userName}\" Pivot=\"{def.Name}\" PivotType={def.PivotType} Collection=\"{collectionName}\" ExtraFilter=\"{extraFilter}\" executed in Elapsed=\"{sw.Elapsed}\"" );
#pragma warning restore CS8604 // Possible null reference argument.
        }
    }

    public async Task<ArrayBasedPivotData?> GetCachedResultAsync(string collectionName,  PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, Func<string, PivotDefinition, FilterExpressionTree.ExpressionGroup?, string> getQueryText, CancellationToken token = default )
    {
        try
        {
            var id = GetCacheKey(collectionName, def, extraFilter, getQueryText );

            var coll = GetCollectionWithRetries<ArrayBasedPivotData>( CacheCollectionName );
            var c    = coll.Find( new BsonDocument {{"_id", id}} );
            var doc  = await c.FirstOrDefaultAsync(cancellationToken: token);
            doc?.ReorderColumns( def.ColumnsOrder );
            return doc;
        }
        catch ( Exception )
        {
            // ignore
            return null;
        }
    }

    private static string GetCacheKey(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, Func<string, PivotDefinition, FilterExpressionTree.ExpressionGroup?, string> getQueryText)
    {
        var text = getQueryText(collectionName, def, extraFilter );
        using var hash = MD5.Create();
        hash.Initialize();
        var bytes = Encoding.UTF8.GetBytes( text );
        hash.TransformFinalBlock( bytes, 0, bytes.Length );
        var name = $"{collectionName}-{def.Name}-{BitConverter.ToString( hash.Hash! ).Replace( "-", "" )}";

        return name;
    }

    public async Task CacheResultsAsync(string collectionName, ArrayBasedPivotData data, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, Func<string, PivotDefinition, FilterExpressionTree.ExpressionGroup?, string> getQueryText, CancellationToken token = default )
    {
        var id = GetCacheKey(collectionName, def, extraFilter, getQueryText);
        data.Id = id;

        if ( data.Count == 0 || data.Count > MaxCachedRows )
            return;

        try
        {
            data.ExpireAt = DateTime.UtcNow + TimeSpan.FromHours( PivotCacheTTLHours );

            var coll   = GetCollectionWithRetries<ArrayBasedPivotData>(CacheCollectionName);
            var filter = $"{{_id : \"{id}\" }}";
            try
            {
                await coll.FindOneAndDeleteAsync( filter, cancellationToken: token);
            }
            catch ( Exception )
            {
                // ignore
            }

            await coll.InsertOneAsync( data, new() {BypassDocumentValidation = true}, token);
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public Task<string> GetQueryTextAsync(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default ) => Task.FromResult( GetQueryText(collectionName, def, extraFilter ) );

    private string GetQueryText(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter) =>
        GetPivotPipelineText(collectionName, def, extraFilter );
        //"""
        //    db.getCollection("[COLLECTION]").aggregate(
        //    [TEXT]
        //    )
        //    """
        //    .Replace( "[COLLECTION]", collectionName)
        //    .Replace( "[TEXT]",       GetPivotPipelineText(collectionName, def, extraFilter ) );

    private string GetPivotPipelineText(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter )
    {
        try
        {
            var fields = GetFieldMapping(collectionName);

            def = def.Clone();
            foreach ( var f in def.DataFields
                                  .Where( x => fields.IsCalculated( x ) && !string.IsNullOrWhiteSpace( fields.GetLookup( x ) ) )
                                  .Select( x => Tuple.Create( x, fields.GetLookup( x ) ) )
                    )
            {
                if ( !string.IsNullOrWhiteSpace( def.BeforeGrouping ) )
                    def.BeforeGrouping += ",\n";
                def.BeforeGrouping += f.Item2; // lookup def
            }

            var json = def.ToJson(extraFilter, GetAllFields(collectionName), x => fields.GetAggregationOperator( x ) );
            json = RemoveComments(json);

            var pipeline   = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>( json ).Select( p => p.AsBsonDocument ).ToList();

            fields.MapAllFields( pipeline );

            json = pipeline.ToJson( new() {Indent = true, OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson} );

            return json;
        }
        catch
        {
            try
            {
                var json = def.ToJson( extraFilter, GetAllFields(collectionName),x => _fieldMap[collectionName].GetAggregationOperator( x ) );
                json = RemoveComments(json);
                return json;
            }
            catch
            {
                return "";
            }
        }
    }

    private Dictionary<string, Type> GetAllFields(string collectionName)
    {
        if ( !_fieldMap.TryGetValue( collectionName, out var mapping ) || mapping.Count == 0 )
            return [];
        var res = new Dictionary<string, Type>();
        foreach ( var field in mapping.Fields )
        {
            res[field.Key] = field.Value.Type;
        }
        return res;
    }
        
    

    private static string RemoveComments(string input)
    {
        // quick check
        if (input.IndexOf("//", StringComparison.Ordinal) < 0 && input.IndexOf("/*", StringComparison.Ordinal) < 0)
            return input;

        //https://stackoverflow.com/questions/3524317/regex-to-strip-line-comments-from-c-sharp/3524689#3524689

        const string blockComments   = @"/\*(.*?)\*/";
        const string lineComments    = @"//(.*?)\r?\n";
        const string strings         = """
                                       "((\\[^\n]|[^"\n])*)"
                                       """;
        const string verbatimStrings = """@("[^"]*")+""";

        try
        {
            var noComments = Regex.Replace(input,
                                           blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                                           me =>
                                           {
                                               if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                                                   return me.Value.StartsWith("//") ? Environment.NewLine : "";
                                               // Keep the literal strings
                                               return me.Value;
                                           },
                                           RegexOptions.Singleline);
            return noComments;
        }
        catch (Exception ex)
        {
            _log.Error("Can't remove comments from this pipeline:\n"+input, ex);
            return input;
        }
    }

    private async Task<ArrayBasedPivotData> AggregationPivotAsync(
        string collectionName, 
        PivotDefinition def, 
        FilterExpressionTree.ExpressionGroup? extraFilter, 
        int maxFetchSize = -1,
        CancellationToken token = default )
    {
        var coll = GetCollectionWithRetries<BsonDocument>( collectionName );

        def.AllowDiskUsage = true;
        var options = new AggregateOptions
        {
            BypassDocumentValidation = true,
            AllowDiskUse             = true
        };

        // preload field mapping. we'll need it in GetPivotPipelineText
        _ = await GetFieldMapping(collectionName, token);

        var json = GetPivotPipelineText(collectionName, def, extraFilter);
        Debug.WriteLine(json);
        var pipeline = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(json)
                              .Select(p => p.AsBsonDocument)
                              .ToList();

        using var           cursor = await coll.AggregateAsync<BsonDocument>( pipeline, options, token);
        var pd     = await FetchPivotData( 
            collectionName, 
            def, 
            cursor, 
            extraFilter, 
            maxFetchSize, 
            false,
            token);

        return pd;
    }

    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IAsyncCursor<T> asyncCursor)
    {
        while (await asyncCursor.MoveNextAsync())
        {
            foreach (var current in asyncCursor.Current)
            {
                yield return current;
            }
        }
    }

    private async Task<ArrayBasedPivotData> FetchPivotData(string                     collectionName,
                                                           PivotDefinition            def,
                                                           IAsyncCursor<BsonDocument> cursor,
                                                           FilterExpressionTree.ExpressionGroup? extraFilter,
                                                           int                        maxFetchSize = -1,
                                                           bool                       includeId    = false,
                                                           CancellationToken          token        = default
    )
    {
        var (pd, _) = await FetchPivotData(
            collectionName, 
            def.Name, 
            _fieldMap, 
            ToAsyncEnumerable(cursor), 
            extraFilter, 
            false, 
            maxFetchSize,
            includeId,
            token);
        pd.ReorderColumns( def.ColumnsOrder );
        return pd;
    }

    public static async Task<(ArrayBasedPivotData, List<BsonDocument>)> FetchPivotData(
        string                                     collectionName,
        string                                     pivotName,
        ConcurrentDictionary<string, FieldMapping> fieldMap,
        IAsyncEnumerable<BsonDocument>             batch,
        FilterExpressionTree.ExpressionGroup?      extraFilter,
        bool                                       needBson     = false,
        int                                        maxFetchSize = -1,
        bool                                       includeId    = false,
        CancellationToken                          token        = default)
    {
        var sw = new Stopwatch();
        sw.Start();

        var pd = new ArrayBasedPivotData( Array.Empty<string>() );

        var headers    = new List<Tuple<string, string>>(); // display text / column in the data set
        var headersSet = new HashSet<string>();             // for speed

        if (includeId)
        {
            headers.Add(Tuple.Create("_id", "_id"));
            headersSet.Add("_id");
            pd.AddHeader("_id");

        }

        List<BsonDocument> documents = [];

        var       docs       = 0;
        await using var enumerator = batch.GetAsyncEnumerator(token);

        while ( await enumerator.MoveNextAsync() )
        {
            var doc = enumerator.Current;
            if ( doc.IsBsonNull )
                continue;

            //{ _id: wateva, values: {Book:a, tenor:1w, value:2}}
            //{ _id: wateva, values: {rows : { anyId1: {Book:a, tenor:1w, value:2}, anyid2: {Book:a, tenor:1y, value:1}} }} }
            
            if (needBson)
                documents.Add(doc);

            var res = doc.ToDictionary();
            var id  = res["_id"] as Dictionary<string, object>;

            // for map/reduce results
            var value = ( res.TryGetValue("value", out var re)
                ? (Dictionary<string, object>)re : null ) ?? [];

            if ( id == null && value.Count == 0 && res.Count == 2 && res.ContainsKey("_id") && res.ContainsKey("value") )
                continue;

            //The columns are sparse, so we have to process every row, looking for new/additional columns to add
            UpdateHeaders(collectionName, res, headersSet, headers, pd, fieldMap);

            if (value.TryGetValue("rows", out var value1))
            {
                var rows = (Dictionary<string, object>)value1;

                //multirow response, this is for optimising large Rho/Vega aggregations
                //{ _id: wateva, values: {rows : { anyId1: {Book:a, tenor:1w, value:2}, anyid2: {Book:a, tenor:1y, value:1}} }} }
                foreach (var row in rows)
                {
                    var rowId = new Dictionary<string, object>
                    {
                        { "_id", row.Key }
                    };
                    var rowValues = (Dictionary<string, object>)row.Value;

                    pd.Add(headers
                          .Select(x => x.Item2)
                          .Select(x => rowId.TryGetValue(x, out var val) || rowValues.TryGetValue(x, out val)
                                      ? val
                                      : null));
                }
            }
            else
            {
                var objects = headers
                   .Select(x => x.Item2)
                   .Select(x => res.TryGetValue(x, out var val) || id != null && id.TryGetValue(x, out val) ||
                                value.TryGetValue(x, out val)
                               ? val
                               : null);

                pd.Add(objects);
            }

            docs++;
            if (docs >= PivotMaxReturnedRows || ( maxFetchSize > 0 && docs >= maxFetchSize) )
            {
                _log.Warn($"Data truncated at PivotMaxReturnedRows. Pivot=\"{pivotName}\", Collection=\"{collectionName}\", ExtraFilter=\"{extraFilter}\" Rows={docs}");
                break;
            }
        }

        if ( pd.Headers.Count == 0 )
        {
            _log.Warn( $"No results fetched for Pivot=\"{pivotName}\"" );
            pd = new(["Message"]);
            pd.Add(["No results"]);
        }

        sw.Stop();
        _log.Debug($"Data fetched. Pivot=\"{pivotName}\", Collection=\"{collectionName}\", ExtraFilter=\"{extraFilter}\" Rows={pd.Count}, Cols={pd.Headers.Count}, ElapsedMs={sw.Elapsed.TotalMilliseconds}");
        return (pd, documents);
    }

    private static void UpdateHeaders(
        string                                     collectionName, 
        Dictionary<string, object?>                document, 
        ISet<string>                               headersSet, 
        ICollection<Tuple<string, string>>         headers,
        ArrayBasedPivotData                        pd,
        ConcurrentDictionary<string, FieldMapping> fieldMap )
    {
        foreach (var kvp in document)
        {
            switch (kvp.Key)
            {
                case "_id":
                    if (kvp.Value is IDictionary<string, object> kvpIDict)
                    {
                        foreach (var key in kvpIDict.Keys)
                        {
                            var unmapped = fieldMap[collectionName].UnmapField(key);
                            if (headersSet.Contains(unmapped))
                                continue;
                            pd.AddHeader(unmapped); // new header!
                            headers.Add(Tuple.Create(unmapped, key));
                            headersSet.Add(unmapped);
                        }
                    }

                    break;
                case "value":
                    if (kvp.Value is Dictionary<string, object> kvpDict)
                    {
                        foreach (var (key, value) in kvpDict)
                        {
                            if (key.ToUpper() == "ROWS")
                            {
                                //this structure allows us to return multiple rows from a single map/reduce
                                //{ _id: wateva, values : {rows : { anyId1: {Book:a, tenor:1w, value:2}, anyid2: {Book:a, tenor:1y, value:1}} }} }
                                foreach (var inner in value as Dictionary<string, object> ?? [])
                                    foreach (var innerInner in inner.Value as Dictionary<string, object> ?? [])
                                    {
                                        var unmapped = fieldMap[collectionName].UnmapField(innerInner.Key);
                                        if (headersSet.Contains(unmapped))
                                            continue;
                                        pd.AddHeader(unmapped); // new header!
                                        headers.Add(Tuple.Create(unmapped, innerInner.Key));
                                        headersSet.Add(unmapped);
                                    }
                            }
                            else
                            {
                                var unmapped = fieldMap[collectionName].UnmapField(key);
                                if (headersSet.Contains(unmapped))
                                    continue;
                                pd.AddHeader(unmapped); // new header!
                                headers.Add(Tuple.Create(unmapped, key));
                                headersSet.Add(unmapped);
                            }
                        }
                    }

                    break;
                default:
                {
                    var unmapped = fieldMap[collectionName].UnmapField(kvp.Key);
                    if (headersSet.Contains(unmapped))
                        continue;
                    pd.AddHeader(unmapped); // new header!
                    headers.Add(Tuple.Create(unmapped, kvp.Key));
                    headersSet.Add(unmapped);
                }
                    break;
            }
        }
    }

    public async Task<List<PivotDefinition>> GetPivotsAsync(string collectionName,
                                                            IPivotTableDataSource.PivotType pivotType,
                                                            string? userName = null, CancellationToken token = default)
    {
        userName = string.IsNullOrWhiteSpace(userName) ? User : userName;

        var coll = GetCollectionWithRetries<PivotDefinitions>($"{collectionName}-Meta");

        //get all the predefined pivots
        var standardPivots = (await coll.Find(x => x.Id.Equals("PredefinedPivots")).FirstOrDefaultAsync(cancellationToken: token))?.Pivots ?? [];
        //get all user pivots
        var allUserPivots  = await coll.Find(x => x.Id.StartsWith("PredefinedPivots-")).ToListAsync(cancellationToken: token) ?? [];
        //find the specific users private pivots from the set of user pivots
        var userPivots     = (allUserPivots.FirstOrDefault(doc => doc.Id.Equals( $"PredefinedPivots-{userName}", StringComparison.OrdinalIgnoreCase)))?.Pivots ?? [];

        // fix accidental saves where a user pivot is saved into the global section
        foreach (var def in standardPivots.Where(def => def.Group == PivotDefinition.UserPivotsGroup))
        {
            def.Group = PivotDefinition.PredefinedPivotsGroup;
        }

        // Change group for user pivots to match the user's name
        foreach (var pivots in allUserPivots)
        {
            var name = pivots.Id.StartsWith("PredefinedPivots-")
                ? pivots.Id["PredefinedPivots-".Length..]
                : string.Empty;

            if (string.IsNullOrEmpty(name)) 
                continue;

            foreach (var def in pivots.Pivots)
                def.Group = $"User pivots - {name}";
        }

        var res = pivotType switch
        {
            IPivotTableDataSource.PivotType.Predefined        => standardPivots,
            IPivotTableDataSource.PivotType.User              => userPivots,
            IPivotTableDataSource.PivotType.UserAndPredefined => standardPivots.Concat(userPivots),
            IPivotTableDataSource.PivotType.All               => standardPivots.Concat(allUserPivots.SelectMany(x => x.Pivots)),
            _ => throw new ArgumentOutOfRangeException(nameof(pivotType), pivotType, null)
        };

        var result = res
            .OrderBy(x => (x.Group, x.Name))
            .ToList()
            ;

        return result;
    }


    public async Task UpdatePredefinedPivotsAsync(string collectionName, IEnumerable<PivotDefinition> pivots, bool predefined = false, string? userName = null, CancellationToken token = default )
    {
        var coll = GetCollectionWithRetries<PivotDefinitions>($"{collectionName}-Meta");

        PivotMetaCache.Reset(this, collectionName);

        userName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName; 

        var id = predefined 
                ? "PredefinedPivots" 
                : $"PredefinedPivots-{userName}"
            ;

        var doc = await coll.Find( new BsonDocument {{"_id", id}} )
                            .FirstOrDefaultAsync(cancellationToken: token)
             ?? new PivotDefinitions
                {
                    Id     = id,
                    Pivots = []
                }
            ;

        foreach (var p in pivots)
        {
            var existing = doc.Pivots.FirstOrDefault(x => x.Name == p.Name && x.Group == p.Group);
            if (existing == null)
            {
                p.Owner = userName;
                doc.Pivots.Add(p);
            }
            else
            {
                // retain order for easier git diffs
                var idx = doc.Pivots.IndexOf(existing);
                p.Owner         = userName;
                doc.Pivots[idx] = p;
            }
        }

        var options = new ReplaceOptions
        {
            IsUpsert = true
        };

        await coll.ReplaceOneAsync( new BsonDocument {{"_id", id}}, doc, options, token);
    }

    // ReSharper disable MemberCanBePrivate.Local
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    // ReSharper disable CollectionNeverUpdated.Local
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local
    // ReSharper disable UnusedMember.Local

    [BsonIgnoreExtraElements]
    // ReSharper disable once ClassNeverInstantiated.Local
    private class InternalPivotColumnDescriptor
    {
        public override string ToString() => $"{NameRegex} {Background} {Format}";

        public string  NameRegex           { get; set; } = "";
        public string? Background          { get; set; }
        public string? AlternateBackground { get; set; }
        public string? Format              { get; set; }

        public PivotColumnDescriptor ToPivotColumnDescriptor() =>
            new()
            {
                NameRegexString = NameRegex,
                // ReSharper disable PossibleNullReferenceException
                Background = ColorConverter.ConvertFromString( Background ?? "White" ), AlternateBackground = ColorConverter.ConvertFromString( AlternateBackground ?? "White" ),
                // ReSharper restore PossibleNullReferenceException
                Format = Format ?? ""
            };
    }

    [BsonIgnoreExtraElements]
    // ReSharper disable once ClassNeverInstantiated.Local
    private class InternalPivotColumnDescriptors
    {
        [BsonId] public string Id { get; set; } = "";

        public List<InternalPivotColumnDescriptor> Fields { get; set; } = [];
    }

    // ReSharper restore UnusedMember.Local
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local
    // ReSharper restore CollectionNeverUpdated.Local
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    // ReSharper restore MemberCanBePrivate.Local

    public async Task<PivotColumnDescriptor[]> GetColumnDescriptorsAsync(string collectionName, CancellationToken token = default)
    {
        var coll = GetCollectionWithRetries<InternalPivotColumnDescriptors>($"{collectionName}-Meta");
        var descriptors = await coll.Find( new BsonDocument {{"_id", "FieldDescriptors"}} ).FirstOrDefaultAsync(cancellationToken: token);

        if ( descriptors == null )
        {
            coll = GetCollectionWithRetries<InternalPivotColumnDescriptors>("Global-Meta");
            descriptors = await coll.Find( new BsonDocument {{"_id", "FieldDescriptors"}} ).FirstOrDefaultAsync(cancellationToken: token);
        }

        return descriptors?.Fields.Select( x => x.ToPivotColumnDescriptor() ).ToArray() ?? [];
    }

    public Dictionary<string, PivotFieldDescriptor> GetFieldTypes(string collectionName)
    {
        var fields = _fieldMap[collectionName];
        var data = fields.Fields
                                           .Select( x => new KeyValuePair<string, Type>( x.Key, x.Value.Type ) )
                                           .Concat( fields.CalculatedFields.Where( x => !fields.ContainsKey( x ) ).Select( x => new KeyValuePair<string, Type>( x, typeof(double) ) ) );

        var res = new Dictionary<string, PivotFieldDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in data)
        {
            if (res.ContainsKey(item.Key))
                continue;
            res[item.Key] = new()
            {
                Name    = item.Key,
                Purpose = GetPurpose(collectionName, item.Key),
                Type    = GetFieldType(collectionName, item.Key)
            };
        }

        return res;
    }

    private Type GetFieldType(string collectionName, string key ) =>
        !_fieldMap[collectionName].TryGetValue( key, out var m )
            ? typeof(double)
            : m?.Type ?? typeof(object);

    private PivotFieldPurpose GetPurpose(string collectionName, string key ) =>
        !_fieldMap[collectionName].TryGetValue( key, out var m )
            ? PivotFieldPurpose.Data
            : m?.Purpose ?? PivotFieldPurpose.Data;

    [BsonIgnoreExtraElements]
    private class WindowStateDesc
    {
        [BsonId] public string Id { get; set; } = "";

        public int        Width               { get; set; }
        public int        Height              { get; set; }
        public int        Left                { get; set; }
        public int        Top                 { get; set; }
        public string?    WindowState         { get; set; }
        public bool       AutoRunEnabled      { get; set; }
        public bool       ExpandTable         { get; set; }
        public bool       ExpandDataFields    { get; set; }
        public string []? SelectedDepartments { get; set; }
        public bool       ShowCobRange        { get; set; }
        public string?    DefaultCollection   { get; set; }

    }

    /// <inheritdoc />
    /// <summary>
    /// Get a single document
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="keys">Primary key fields in no particular order</param>
    /// <param name="extraFilter">Extra $match stage</param>
    /// <param name="token"></param>
    /// <returns>Json string</returns>
    public Task<string> GetDocumentAsync(string collectionName, KeyValuePair<string, object> [] keys, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default )
    {
        var filter = new FilterExpressionTree.ExpressionGroup();

        filter.Children.AddRange( 
                keys.Select( x => new FilterExpressionTree.FieldExpression()
                { 
                    Field = x.Key, 
                    Argument = x.Value.ToString() ?? "", 
                    Condition = FilterExpressionTree.FieldConditionType.EqualTo
                } )
            );

        if ( extraFilter != null )
            filter.Children.AddRange( extraFilter.Children );

        return GetDocumentAsync(collectionName, filter, token);
    }

    /// <inheritdoc />
    public async Task<string> GetDocumentAsync(string collectionName, FilterExpressionTree.ExpressionGroup extraFilter, CancellationToken token = default )
    {
        var filter = BsonDocument.Parse( extraFilter.ToJson(GetAllFields(collectionName)) );

        var coll = GetCollectionWithRetries<BsonDocument>(collectionName);
        var res = await (await coll.FindAsync( filter, cancellationToken: token)).FirstOrDefaultAsync(cancellationToken: token);

        return res?.ToJson(new() { Indent = true }) ?? $"Not found: {extraFilter}";
    }

    public async Task DeletePivotAsync(string collectionName, string pivotName, string groupName, string userName, CancellationToken token = default)
    {
        _log.Debug($"Deleting Pivot=\"{pivotName}\" from Group=\"{groupName}\" for User=\"{userName}\"...");

        if (groupName != PivotDefinition.UserPivotsGroup)
            throw new ApplicationException($"You can only delete from \"{PivotDefinition.UserPivotsGroup}\" group");

        var coll = GetCollectionWithRetries<PivotDefinitions>($"{collectionName}-Meta");

        userName = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName; 

        var id = $"PredefinedPivots-{userName}";

        var doc = await coll.Find( new BsonDocument {{"_id", id}} )
                            .FirstOrDefaultAsync(cancellationToken: token)
             ?? new PivotDefinitions
                {
                    Id     = id,
                    Pivots = []
                }
            ;

        var existing = doc.Pivots.FirstOrDefault(x => !x.IsPredefined && x.Name == pivotName)
             ?? throw new ApplicationException($"Pivot=\"{pivotName}\" from Group=\"{groupName}\" was NOT deleted for User=\"{userName}\" as it was not found")
            ;

        // retain order for easier git diffs
        var idx = doc.Pivots.IndexOf(existing);
        doc.Pivots.RemoveAt(idx);

        var options = new ReplaceOptions
        {
            IsUpsert = true
        };

        await coll.ReplaceOneAsync( new BsonDocument {{"_id", id}}, doc, options, token);

        _log.Info($"Pivot=\"{pivotName}\" from Group=\"{groupName}\" deleted for User=\"{userName}\"");
    }

    internal IMongoCollection<T> GetCollectionWithRetries<T>(string name) where T: class =>
        RetryHelper.DoWithRetries(() =>
            {
                if (_database == null || !MongoDbHelper.IsConnected(_database))
                    _database = null;
                return Database.GetCollection<T>(name);
            }, 
            3, 
            TimeSpan.FromSeconds(5), logFunc: LogMethod);

    /// <summary>
    /// Log method for retry function, called for each iteration
    /// </summary>
    /// <param name="iteration"></param>
    /// <param name="maxRetries"></param>
    /// <param name="e"></param>
    private static void LogMethod(int iteration, int maxRetries, Exception e)
    {
        if (iteration < maxRetries - 1)
            _log.Warn($"MongoDB error, retrying  RetriesLeft={maxRetries-iteration-1}\"", e);
        else
            _log.Error("MongoDB error, retries exhausted", e);
    }

}