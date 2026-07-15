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
﻿using System.Diagnostics;
using System.Reflection;
using log4net;
using static Rms.Risk.Mango.Pivot.Core.IPivotTableDataSource;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public static class PivotMetaCache
{
    private static readonly ILog          _log  = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public const string Any = "<Any>";

    public static async Task<bool> PreloadCollections(this IPivotTableDataSourceMetaProvider pivotService, List<GroupedCollection> collections, string? userEmail = null, CancellationToken token = default)
    {
        if ( pivotService == null )
            throw new ArgumentNullException(nameof(pivotService));
        if ( collections == null )
            throw new ArgumentNullException(nameof(collections));

        if ( string.IsNullOrWhiteSpace(pivotService.Prefix) )
            throw new ArgumentException("Pivot service must have a valid Prefix", nameof(pivotService));

        if ( collections.Count == 0 )
        {
            var coll = await LoadCollections(pivotService, token);
            collections.AddRange(coll);
        }

        var changed = 0;
            
        var collectionsToLoad = collections
           .Where(x => 
                   x is { IsGroup: false } 
                && x.DataSourcePrefix == pivotService.Prefix
                && x.Pivots.Count == 0
            )
           .ToList();

        if ( collectionsToLoad.Count == 0 )
            return false;

        var sw = Stopwatch.StartNew();

        _log.Debug($"{pivotService.GetType().Name}: Waiting for lock within PreloadCollections...");

        await _lock.WaitAsync(token);

        if ( sw.Elapsed > TimeSpan.FromSeconds(30) )
            _log.Warn($"Waited {sw.Elapsed} for lock within PreloadCollections");

        try
        {
            var tasks = collectionsToLoad.Select(x => LoadPivots(pivotService, x, PivotType.UserAndPredefined, userEmail, token))
                .ToArray();

            await Task.WhenAll(tasks);
            changed += collectionsToLoad.Sum(x => x.Pivots.Count);

            if ( changed > 0 )
                _log.Debug($"Finished preloading {collections.Count} collections, {changed} pivots for {pivotService.SourceId}");

            return changed > 0;
        }
        finally
        {
            _lock.Release();
            _log.Debug($"{pivotService.GetType().Name}: Released lock within PreloadCollections. Elapsed=\"{sw.Elapsed}\"");
        }
    }

    public static void Clear() => _collectionsCache?.Clear();

    public static void Reset(IPivotTableDataSourceMetaProvider pivotService, string collection)
    {
        var id = MakeId(pivotService, collection);
        _collectionsCache?.Remove(id);
    }




    private static async Task<List<GroupedCollection>> LoadCollections(IPivotTableDataSourceMetaProvider pivotService, CancellationToken token = default)
    {
        var collectionNames = (await pivotService.GetCollectionsAsync(CollectionType.HaveMeta, token))
            .Distinct()
            .OrderBy(x => x)
            .ToArray()
            ;

        if (collectionNames.Length == 0)
            throw new ApplicationException("No collections found");

        var d = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var coll in collectionNames)
        {
            var s = coll.Split(':');

            string group;
            string name;

            if (s.Length != 2)
            {
                group = pivotService.Prefix;
                name  = coll;
            }
            else
            {
                group = s[0].Trim();
                name  = s[1].Trim();
            }

            name = name.Replace("-Meta", "", StringComparison.InvariantCultureIgnoreCase);

            if (d.TryGetValue(group, out var thisGroup))
                thisGroup.Add(name);
            else
                d[group] = [name];
        }

        var keys = d.Keys.ToArray();
        Array.Sort(keys);
        var collections = new List<GroupedCollection>();

        foreach (var k in keys)
        {
            collections.Add(new()
            {
                DataSourcePrefix    = k,
                CollectionNameWithoutPrefix = k,
                IsGroup = true
            });
            collections.AddRange(d[k]
                                    .OrderBy(x => x)
                                    .Select(x => new GroupedCollection
                                     {
                                         DataSourcePrefix            = k,
                                         CollectionNameWithoutPrefix = x,
                                         IsGroup                     = false
                                     }));
        }

        return collections;
    }

    private record LoaderArg(
        IPivotTableDataSourceMetaProvider           PivotService,
        PivotType                       PivotType,
        string                          CollectionNameWithoutPrefix,
        string?                         UserName
        );

    private static ExpiringObjectPool<string, GroupedCollection, LoaderArg>? _collectionsCache;

    private static async Task LoadPivots(
        IPivotTableDataSourceMetaProvider           pivotService,
        GroupedCollection               collection,
        PivotType                       pivotType,
        string?                         userName  = null,
        CancellationToken               token     = default
    )
    {
        if ( collection.IsGroup )
            return;

        _collectionsCache ??= new(LoadPivotsInternal);

        var id = MakeId(pivotService, collection.CollectionNameWithoutPrefix);

        var coll = await _collectionsCache.Get(id, new (pivotService, PivotType.All, collection.CollectionNameWithoutPrefix, userName), token);
        collection.CopyFrom(coll);
        
        var allPivots        = coll.Pivots.Where(x => !x.IsGroup).Select(x => x.Pivot).ToList();
        var predefinedPivots = allPivots.Where(x => !x.Group.StartsWith("User "));
        var thisUserPivots = string.IsNullOrWhiteSpace(userName) ? [] : allPivots.Where(x =>  x.Group.StartsWith("User ") && x.Group.EndsWith(userName));

        var res = pivotType switch
        {
            PivotType.Predefined        => predefinedPivots,
            PivotType.User              => thisUserPivots,
            PivotType.UserAndPredefined => predefinedPivots.Concat(thisUserPivots),
            PivotType.All               => allPivots,
            _ => throw new ArgumentOutOfRangeException(nameof(pivotType), pivotType, null)
        };

        var pivots = res
               .OrderBy(x => (x.Group, x.Name))
               .ToList()
            ;

        collection.Pivots = MakeGroupedPivots(pivots);
    }

    private static string MakeId(IPivotTableDataSourceMetaProvider pivotService, string collection) => $"Collection=\"{collection}\" {pivotService.SourceId}";

    private static async Task<GroupedCollection> LoadPivotsInternal(string key, LoaderArg args, CancellationToken token)
    {
        var pivotService = args.PivotService;
        var pivotType = args.PivotType;
        var userName  = args.UserName;

        List<PivotDefinition>  ? pivotDefinitions = null;
        HashSet<string>        ? allKeyFields     = null;
        HashSet<string>        ? allDataFields    = null;
        PivotColumnDescriptor[]? descriptors      = null;

        var collection = new GroupedCollection
        { 
            CollectionNameWithoutPrefix = args.CollectionNameWithoutPrefix, 
            DataSourcePrefix            = args.PivotService.Prefix, 
            IsGroup                     = false 
        };

        await Task.WhenAll(
            LoadPivotDefinitions(),
            LoadKeyFields(),
            LoadDataFields(),
            LoadColumnDescriptors()
        );

        var fieldTypes = pivotService.GetFieldTypes(collection.CollectionNameWithoutPrefix);
        var pivots     = MakeGroupedPivots(pivotDefinitions);

        collection.Pivots = pivots;

        if ( allKeyFields != null )
            collection.KeyFields = allKeyFields;
        if ( allDataFields != null )
            collection.DataFields = allDataFields;
        if ( descriptors != null )
            collection.ColumnDescriptors = descriptors;
        if ( fieldTypes.Count > 0 )
            collection.FieldTypes = fieldTypes;

        return collection;

        async Task LoadPivotDefinitions()
        {
            if ( collection.Pivots.Count > 0 )
                return;
            pivotDefinitions = await pivotService.GetPivotsAsync(collection.CollectionNameWithoutPrefix, pivotType, userName, token);
        }

        async Task LoadKeyFields()
        {
            if ( collection.KeyFields.Count > 0 )
                return;
            allKeyFields = [..await pivotService.GetKeyFieldsAsync(collection.CollectionNameWithoutPrefix, token)];
        }

        async Task LoadDataFields()
        {
            if ( collection.DataFields.Count > 0 )
                return;
            allDataFields = [..await pivotService.GetDataFieldsAsync(collection.CollectionNameWithoutPrefix, token)];
        }

        async Task LoadColumnDescriptors()
        {
            if ( collection.ColumnDescriptors.Length > 0 )
                return;
            descriptors = await pivotService.GetColumnDescriptorsAsync(collection.CollectionNameWithoutPrefix, token);
        }
    }

    private static List<GroupedPivot> MakeGroupedPivots(List<PivotDefinition>? pivotDefinitions)
    {
        if ( pivotDefinitions == null || pivotDefinitions.Count == 0 )
        {
            return NoPivotsFound();
        }

        var d = new Dictionary<string, List<PivotDefinition>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pivot in pivotDefinitions.Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name != "<Current>"))
        {
            var group = pivot.Group;

            if (d.TryGetValue(group, out var thisGroup))
                thisGroup.Add(pivot);
            else
                d[group] = [pivot];
        }

        var keys = d.Keys.ToArray();
        Array.Sort(keys);
        var pivots = new List<GroupedPivot>();

        foreach (var k in keys)
        {
            pivots.Add(new() {Text = k, Pivot = new(), IsGroup = true});
            pivots.AddRange(d[k]
                               .OrderBy(x => x.Name)
                               .Select(x => new GroupedPivot
                                {
                                    Text    = x.Name,
                                    IsGroup = false,
                                    Pivot   = x
                                }));
        }

        return pivots.Count == 0
                ? NoPivotsFound()
                : pivots
            ;
    }

    private static List<GroupedPivot> NoPivotsFound() =>
    [
        new()
        {
            IsGroup = true,
            Text    = "No pivots found",
            Pivot   = new()
        }
    ];
}