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

namespace Rms.Risk.Mango.Pivot.UI.Services;

public class DummyDataSource : IPivotTableDataSource, IPivotTableDataSourceMetaProvider
{
    public Task   InitAsync(string collectionName, bool skipCache, CancellationToken token = default)                                                                                            => Task.CompletedTask;
    public string SourceId => "Dummy";
    public string Prefix   => "";

    public string User     { get; set; } = string.Empty;

    public Task<List<GroupedCollection>> GetAllMeta(bool force = false, CancellationToken token = default) => Task.FromResult(new List<GroupedCollection>());
    public Task<string[]> GetCollectionsAsync(CollectionType includeMeta = CollectionType.All, CancellationToken token = default) => Task.FromResult<string[]>([]);
    public Task<string[]> GetCobDatesAsync(string               collectionName, bool              force = false, CancellationToken token = default) => Task.FromResult(Array.Empty<string>());
    public Task<string[]> GetDepartmentsAsync(string            collectionName, CancellationToken token = default) => Task.FromResult(Array.Empty<string>());
    public Task<(string, string)[]> GetDesksWithDepartmentAsync(string    collectionName, CancellationToken token = default) => Task.FromResult(Array.Empty<(string, string)>());

    public Task<string[]>                           GetKeyFieldsAsync(string          collectionName, CancellationToken token                             = default) => Task.FromResult(Array.Empty<string>());
    public Task<string[]>                           GetDrilldownKeyFieldsAsync(string collectionName, PivotFieldPurpose keyLevel, CancellationToken token = default) => Task.FromResult(Array.Empty<string>());
    public Task<string[]>                           GetDataFieldsAsync(string         collectionName, CancellationToken token = default) => Task.FromResult(Array.Empty<string>());
    public Task<PivotColumnDescriptor[]>            GetColumnDescriptorsAsync(string  collectionName, CancellationToken token = default) => Task.FromResult(Array.Empty<PivotColumnDescriptor>());
    public Dictionary<string, PivotFieldDescriptor> GetFieldTypes(string              collectionName)                                                                                                          => [];
    public Task<string>                             GetDrilldownAsync(string          collectionName, string          name, string  value = "\"\"", bool equals = false, CancellationToken token    = default) => Task.FromResult("");
    public Task<IPivotedData>                                   PivotAsync(
        string                 collectionName, PivotDefinition def,  FilterExpressionTree.ExpressionGroup? extraFilter,    bool skipCache,
        string?           userName = null, int maxFetchSize = -1, CancellationToken token = default)            => Task.FromResult((IPivotedData)new ArrayBasedPivotData( Array.Empty<string>() ));
    public Task<List<PivotDefinition>> GetPivotsAsync(string collectionName, IPivotTableDataSource.PivotType pivotType,
                                                      string? userName = null, CancellationToken token = default)                                              => Task.FromResult( new List<PivotDefinition>() );
    public Task UpdatePredefinedPivotsAsync(string collectionName, IEnumerable<PivotDefinition> pivots, bool predefined    = false, string? userName = null, CancellationToken token = default) => Task.CompletedTask;
    public Task<string> GetQueryTextAsync(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default)                                                   => Task.FromResult("");
    public Task<string> GetDocumentAsync(string collectionName, KeyValuePair<string, object>[] keys, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default)                                    => Task.FromResult("");
    public Task<string> GetDocumentAsync(string collectionName, FilterExpressionTree.ExpressionGroup extraFilter, CancellationToken token = default)                                                                          => Task.FromResult("");
    public Task DeletePivotAsync(string collectionName, string pivotName, string groupName, string? userName, CancellationToken token = default)                                                => Task.CompletedTask;
}