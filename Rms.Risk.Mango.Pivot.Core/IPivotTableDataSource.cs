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
using Newtonsoft.Json;
using Rms.Risk.Mango.Pivot.Core.Models;
using System.Drawing;
using System.Text.RegularExpressions;
using static Rms.Risk.Mango.Pivot.Core.IPivotTableDataSource;

namespace Rms.Risk.Mango.Pivot.Core;

public class PivotColumnDescriptor
{
    [JsonIgnore] public Regex  NameRegex           { get; private set; } = null!;
    public              Color  Background          { get; set; }
    public              Color  AlternateBackground { get; set; }
    public              string Format              { get; set; } = "";
    public              bool   ShowTotals          { get; set; } = true;

    public string NameRegexString
    {
        get;
        set
        {
            field = value;
            NameRegex = new(
                value,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase |
                RegexOptions.Singleline
            );
        }
    } = "";
}

public enum PivotFieldPurpose
{
    Data        = 0,
    PrimaryKey1 = 1,
    PrimaryKey2 = 2,
    Key         = 3,
    Info        = 4,
    Hidden      = 5
}

public class PivotFieldDescriptor
{
    public string            Name    { get; set; } = "";
    public PivotFieldPurpose Purpose { get; set; }

    [JsonIgnore] public Type Type { get; set; } = typeof(object);

    public string TypeString 
    {
        get => Type.Name;
        set => Type = Type.GetType( "System."+value ) ?? throw new ("Type not found: System."+value);
    }
}

public enum CollectionType
{
    All,
    NoMeta,
    HaveMeta
}

public class GroupedPivot : ICloneable
{
    public required string          Text    { get; init; }
    public          bool            IsGroup { get; init; }
    public required PivotDefinition Pivot   { get; init; }

    public override string ToString() => Text;

    object ICloneable.Clone() => Clone();

    public GroupedPivot Clone() =>
        new()
        {
            Text    = Text,
            IsGroup = IsGroup,
            Pivot   = Pivot.Clone()
        };
}

public class GroupedCollection
{
    public required string                                      DataSourcePrefix            { get; init; }
    public          string                                      CollectionNameWithPrefix    => $"{DataSourcePrefix}: {CollectionNameWithoutPrefix}";
    public required string                                      CollectionNameWithoutPrefix { get; init; }
    public          bool                                        IsGroup                     { get; init; }
    public          PivotColumnDescriptor[]                     ColumnDescriptors           { get; set; } = [];
    public          HashSet<string>                             DataFields                  { get; set; } = [];
    public          HashSet<string>                             KeyFields                   { get; set; } = [];
    public          List<GroupedPivot>                          Pivots                      { get; set; } = [];
    public          Dictionary<string, PivotFieldDescriptor>    FieldTypes                  { get; set; } = [];

    public override string ToString() => CollectionNameWithPrefix;

    public void CopyFrom(GroupedCollection other)
    {
        if ( other == null            ) throw new ArgumentNullException(nameof(other));

        ColumnDescriptors = other.ColumnDescriptors;
        DataFields        = other.DataFields;
        KeyFields         = other.KeyFields;
        Pivots            = other.Pivots;
        FieldTypes        = other.FieldTypes;
    }

    public List<string> GetDrilldownKeyFields(PivotFieldPurpose purpose) =>
        FieldTypes
           .Where( x => x.Value.Purpose == purpose )
           .Select(x => x.Key)
           .OrderBy(x => x)
           .ToList();
}

public interface IPivotTableDataSource
{
    string SourceId { get; }
    string Prefix   { get; }

    string User { get; set; }

    Task<List<GroupedCollection>> GetAllMeta(bool force = false, CancellationToken token = default);

    /// <summary>
    /// Get drilldown formula for the given column
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="name"></param>
    /// <param name="value">Value to be compared with. Only records not matching this value will be shown.</param>
    /// <param name="equals">If true "name = value", if false "name != value"</param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task<string> GetDrilldownAsync(string collectionName, string name, string value = "\"\"", bool equals = false, CancellationToken token = default );

    /// <summary>
    /// Aggregate data
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="def">Pivot definition</param>
    /// <param name="extraFilter">Extra $match stage</param>
    /// <param name="skipCache">Skip cached results</param>
    /// <param name="userName"></param>
    /// <param name="maxFetchSize"></param>
    /// <param name="token"></param>
    /// <returns>Pivoted data</returns>
    Task<IPivotedData> PivotAsync(
        string                                collectionName,
        PivotDefinition                       def,
        FilterExpressionTree.ExpressionGroup? extraFilter,
        bool                                  skipCache,
        string?                               userName     = null,
        int                                   maxFetchSize = -1,
        CancellationToken                     token        = default
        );

    public enum PivotType
    {
        Predefined,
        User,
        UserAndPredefined,
        All
    }

    Task UpdatePredefinedPivotsAsync(string                       collectionName,
                                     IEnumerable<PivotDefinition> pivots,
                                     bool                         predefined = false,
                                     string? userName   = null,
                                     CancellationToken            token      = default);

    Task UpdatePivotAsync(string            collectionName,
                          PivotDefinition   pivot,
                          string? userName = null,
                          CancellationToken token    = default)
        => UpdatePredefinedPivotsAsync(collectionName, [pivot], pivot.IsPredefined, userName, token);
        
    /// <summary>
    /// Preprocess def to get proper query text.
    /// Should perform all sort of postprocessing appied to the query def.
    /// </summary>
    /// <param name="collectionName">mongo collection Name</param>
    /// <param name="def">Pivot definition</param>
    /// <param name="extraFilter">Extra $match stage</param>
    /// <param name="token"></param>
    /// <returns>Processed query</returns>
    Task<string> GetQueryTextAsync(string collectionName, PivotDefinition def, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default );

    /// <summary>
    /// Get a single document
    /// </summary>
    /// <param name="collectionName">mongo collection Name</param>
    /// <param name="keys">Primary key fields in no particular order</param>
    /// <param name="extraFilter">Extra $match stage</param>
    /// <param name="token"></param>
    /// <returns>Json string</returns>
    Task<string> GetDocumentAsync(string collectionName, KeyValuePair<string, object> [] keys, FilterExpressionTree.ExpressionGroup? extraFilter, CancellationToken token = default);

    /// <summary>
    /// Get a single document
    /// </summary>
    /// <param name="collectionName">mongo collection Name</param>
    /// <param name="filterText">Filter selecting a single document</param>
    /// <param name="token"></param>
    /// <returns>Json string</returns>
    Task<string> GetDocumentAsync(string collectionName, FilterExpressionTree.ExpressionGroup filterText, CancellationToken token = default);

    /// <summary>
    /// Delete pivot from the collection. Works for user pivots only.
    /// </summary>
    /// <param name="collectionName">mongo collection Name</param>
    /// <param name="pivotName">Pivot to delete</param>
    /// <param name="groupName">Group pivot belongs to. Currently only "User Pivots"</param>
    /// <param name="userName">User who owns the pivot</param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task DeletePivotAsync(string collectionName, string pivotName, string groupName, string userName, CancellationToken token = default);
}

/// <summary>
/// Low level metadata access interface. Should not be used directly in the application. Intended to be used internal caches only. <see cref="PivotMetaCache"/>
/// Applications should use <see cref="IPivotTableDataSource.GetAllMeta(bool, CancellationToken)"/> instead.
/// </summary>
public interface IPivotTableDataSourceMetaProvider
{
    string SourceId { get; }
    string Prefix   { get; }

    Task<string[]>                GetCollectionsAsync(CollectionType includeMeta = CollectionType.All, CancellationToken token = default);
    Task<string[]>                GetDepartmentsAsync(string            collectionName, CancellationToken token                             = default);
    Task<(string, string)[]>      GetDesksWithDepartmentAsync(string    collectionName, CancellationToken token                             = default);
    Task<string[]>                GetKeyFieldsAsync(string              collectionName, CancellationToken token                             = default);
    Task<string[]>                GetDrilldownKeyFieldsAsync(string     collectionName, PivotFieldPurpose keyLevel, CancellationToken token = default);
    Task<string[]>                GetDataFieldsAsync(string             collectionName, CancellationToken token = default);
    Task<PivotColumnDescriptor[]> GetColumnDescriptorsAsync(string      collectionName, CancellationToken token = default);
    Task<string[]>                GetCobDatesAsync(string               collectionName, bool force = false,CancellationToken token = default);

    /// <summary>
    /// Get all field types including keys, data and calculated fields
    /// </summary>
    /// <returns>Field types</returns>
    Dictionary<string, PivotFieldDescriptor> GetFieldTypes(string collectionName);

    Task<List<PivotDefinition>> GetPivotsAsync(string collectionName, PivotType pivotType, string? userName = null, CancellationToken token = default);

}
