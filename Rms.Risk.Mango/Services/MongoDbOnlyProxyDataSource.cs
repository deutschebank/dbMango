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
﻿using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.Core.Models;
using Rms.Risk.Mango.Pivot.UI.Services;

namespace Rms.Risk.Mango.Services;

public class MongoDbOnlyProxyDataSource(IPivotTableDataSource mongoDb) : IPivotTableDataSource, IPivotTableDataSourceMetaProvider
{
    //private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    // public static int ConnectionAttempts        = 3;
    // public static int ConnectionAttemptDelaySec = 3;

    public  string                SourceId => MongoDb.SourceId;
    public  string                Prefix   => "";
    private IPivotTableDataSource MongoDb  => mongoDb;
    private IPivotTableDataSourceMetaProvider MongoDbMeta  => (IPivotTableDataSourceMetaProvider)mongoDb;

    private IPivotTableDataSource ClickHouse
    {
        get;
    } = new DummyDataSource();

    private IPivotTableDataSourceMetaProvider ClickHouseMeta => (IPivotTableDataSourceMetaProvider)ClickHouse;

    public string User
    {
        get => MongoDb.User;
        set
        {
            MongoDb.User    = value;
            ClickHouse.User = value;
        }
    }

    private class ParsedCollectionName
    {
        public bool IsClickHouse    { get; init; }
        public required string Name { get; init; }
    }

    private static ParsedCollectionName ParseCollectionName(string collectionName)
    {
        var s = collectionName.Split(":");
        if (s.Length == 1)
            s = ["Forge", s[0].Trim()];
        if (s.Length != 2)
            throw new ApplicationException($"Invalid CollectionName=\"{collectionName}\"");

        return new()
        {
            IsClickHouse = s[0].Equals("BFG", StringComparison.OrdinalIgnoreCase), 
            Name         = s[1].Trim()
        };
    }

    private bool IsClickHouse { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    private string CollectionName { get; set; } = "";


    public Task<List<GroupedCollection>> GetAllMeta(bool force = false, CancellationToken token = default) => MongoDb.GetAllMeta(force, token);

    public async Task<string[]> GetCollectionsAsync(CollectionType includeMeta = CollectionType.All, CancellationToken token = default)
    {
        var tasks = new[] {MongoDbMeta.GetCollectionsAsync(includeMeta, token), ClickHouseMeta.GetCollectionsAsync(includeMeta, token)};
        await Task.WhenAll(tasks);
        var mongoDbCollections    = tasks[0].Result;
        var clickHouseCollections = tasks[1].Result;

        return mongoDbCollections.Select(x => $"Forge: {x}")
                                 .Concat(clickHouseCollections.Select(x => $"BFG: {x}"))
                                 .ToArray();
    }

    public Task<string[]> GetCobDatesAsync(string collectionName, bool force = false, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetCobDatesAsync(coll.Name, force, token);
    }

    public Task<string[]> GetDepartmentsAsync(string collectionName, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetDepartmentsAsync(coll.Name, token);
    }

    public Task<(string, string)[]> GetDesksWithDepartmentAsync(string collectionName, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetDesksWithDepartmentAsync(coll.Name, token);
    }

    public Task<string[]> GetKeyFieldsAsync(string collectionName, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetKeyFieldsAsync(coll.Name, token);
    }

    public Task<string[]> GetDrilldownKeyFieldsAsync(string collectionName, PivotFieldPurpose keyLevel, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetDrilldownKeyFieldsAsync(coll.Name, keyLevel, token);
    }

    public Task<string[]> GetDataFieldsAsync(string collectionName, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetDataFieldsAsync(coll.Name, token);
    }

    public Task<PivotColumnDescriptor[]> GetColumnDescriptorsAsync(string collectionName, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetColumnDescriptorsAsync(coll.Name, token);
    }

    public Dictionary<string, PivotFieldDescriptor> GetFieldTypes(string collectionName)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetFieldTypes(coll.Name);
    }

    public Task<string> GetDrilldownAsync(string collectionName, string name, string value = "\"\"", bool equals = false, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).GetDrilldownAsync(coll.Name, name, value, equals, token);
    }

    public Task<IPivotedData> PivotAsync(string            collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter,
                                         bool              skipCache, string? userName = null, int maxFetchSize = -1,
                                         CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).PivotAsync(coll.Name, def, extraFilter, skipCache, userName,maxFetchSize, token);
    }

    public Task<List<PivotDefinition>> GetPivotsAsync(string  collectionName, IPivotTableDataSource.PivotType pivotType,
                                                      string? userName = null, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouseMeta : MongoDbMeta).GetPivotsAsync(coll.Name, pivotType, userName, token);
    }

    public Task UpdatePredefinedPivotsAsync(string collectionName, IEnumerable<PivotDefinition> pivots, bool predefined = false,
                                            string? userName = null, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).UpdatePredefinedPivotsAsync(coll.Name, pivots, predefined, userName, token);
    }

    public Task<string> GetQueryTextAsync(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).GetQueryTextAsync(coll.Name, def, extraFilter, token);
    }

    public Task<string> GetDocumentAsync(string collectionName, KeyValuePair<string, object>[] keys, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).GetDocumentAsync(coll.Name, keys, extraFilter, token);
    }

    public Task<string> GetDocumentAsync(string collectionName, FilterExpressionTree.ExpressionGroup extraFilter,  CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).GetDocumentAsync(coll.Name, extraFilter, token);
    }

    public Task DeletePivotAsync(string            collectionName, string pivotName, string groupName, string userName,
                                 CancellationToken token = default)
    {
        var coll = ParseCollectionName(collectionName);
        return (coll.IsClickHouse ? ClickHouse : MongoDb).DeletePivotAsync(coll.Name, pivotName, groupName, userName, token);
    }
}