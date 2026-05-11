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
﻿using System.Reflection;
using log4net;
using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.Core.Models;

/// <summary>
/// Implements drilldown functionality.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DrilldownSupport(List<GroupedCollection> _collections)
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    public const string DefaultDrilldownField = "<Default>";

    public Func<KeyValuePair<string, object>[], Task> ShowDocument        { get; set; } = _ => Task.CompletedTask;
    public Func<string, Task>                         MessageBoxShow      { get; set; } = _ => Task.CompletedTask;
    public Func<string, string, Task<bool>>           MessageBoxShowYesNo { get; set; } = (_,_) => Task.FromResult(false);
    public Func<Exception, Task>                      ShowException       { get; set; } = _ => Task.CompletedTask;
    public Func<string, string, PivotDefinition?>     GetPivotDefinition  { get; set; } = (_,_) => null;
    public Func<string, string, Task<Tuple<string,PivotDefinition>?>> GetCustomDrilldown  { get; set; } = (_,_) => Task.FromResult<Tuple<string,PivotDefinition>?>(null);
    public Func<string, Task<BsonDocument?>>          GetSchema           { get; set; } = (_) => Task.FromResult<BsonDocument?>(null);

    private GroupedCollection GetCollection(string name) => _collections.FirstOrDefault( x => x.CollectionNameWithPrefix == name ) ?? throw new ApplicationException($"Collection=\"{name}\" is not found");

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static bool ShowAllKeysInDrilldowns { get; set; }

    /// <summary>
    /// Determines collection and pivot that needs to be executed.
    /// Returned pivot contains he filter required to show only rows of the original set that
    /// made contribution to the number shown in column/>.
    /// </summary>
    /// <param name="source">Data source</param>
    /// <param name="collectionName">Source collection name</param>
    /// <param name="displayName">Column name as displayed to drilldown to</param>
    /// <param name="displayToRealNameMap">Display name to real column name map</param>
    /// <param name="allColumns">All columns currently shown. Names must be resolvable via getValue</param>
    /// <param name="current">Currently shown pivot definition</param>
    /// <param name="getValue">Get value of any column for the current row</param>
    /// <param name="allDataFields">List of all data fields that can potentially be shown using current pivot</param>
    /// <param name="allKeyFields">List of all key fields that can potentially be shown using current pivot</param>
    /// <returns>Collection name and pivot definition implementing drilldown for supplied field</returns>
    public async Task<Tuple<string, PivotDefinition>?> Drilldown(
        IPivotTableDataSource      source, 
        string                     collectionName, 
        string                     displayName, 
        Dictionary<string, string> displayToRealNameMap, 
        string []                  allColumns,
        PivotDefinition            current, 
        Func<string, object?>      getValue,
        IReadOnlySet<string>       allDataFields,
        IReadOnlySet<string>       allKeyFields
    )
    {
        try
        {
            var fields = GetPrimaryKey2KeyFields(collectionName, getValue, allColumns).ToArray();
            if ( fields.Length == GetCollection(collectionName).FieldTypes.Count( x => x.Value.Purpose == PivotFieldPurpose.PrimaryKey2 ) )
            {
                // all keys already shown. now ony one document can be selected. 
                // show the detailed single document view instead of report

                await ShowDocument( fields! );
                return null;
            }

            var realName = Rename(displayName, displayToRealNameMap);

            var pivotTuple = await GetCustomDrilldown( collectionName, realName );

            pivotTuple ??= await TryCustomDrilldown(source, collectionName, current, getValue, realName, allColumns);

            if ( pivotTuple != null )
                return pivotTuple;

            // aggregation drilldown

            // there is no reason to check this for map/reduce reports as they always contains unique column names
            if ( !allDataFields.Contains( realName ) && !allKeyFields.Contains( realName ) )
            {
                await MessageBoxShow( $"Drilldown is not supported for Column=\"{realName}\" DisplayedAs=\"{displayName}\"" );
                return null;
            }

            // make inverted dictionary
            var realToDisplayNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach ( var item in displayToRealNameMap )
                realToDisplayNameMap[item.Value] = item.Key;

            pivotTuple = await DrilldownInternal(source, collectionName, getValue, displayName, realName, realToDisplayNameMap, current, allKeyFields);
            return pivotTuple;
        }
        catch (Exception ex)
        {
            await ShowException(ex);
            return null;
        }
    }

    private static string Rename(string name, Dictionary<string, string> map)
    {
        if ( !map.TryGetValue(name, out var value) || value == null )
            value = name;
        return value;
    }

    private async Task<Tuple<string, PivotDefinition>?> DrilldownInternal(
        IPivotTableDataSource      dataSource,
        string                     collectionName,
        Func<string, object?>      getValue,
        string                     displayName,
        string                     realName,
        Dictionary<string, string> realToDisplayNameMap,
        PivotDefinition            source,
        IReadOnlySet<string>       allKeys)
    {
        try
        {
            var value    = getValue(displayName);
            var isString = value is string;
            var isDate   = value is DateTime;
            var isSql    = collectionName.StartsWith("BFG: ");

            var keyFields  = source.KeyFields.ToArray();
            var dataFields = source.DataFields.ToArray();

            var current = source.Clone();

            current.Group = PivotDefinition.CurrentPivotGroup;

            var keyFilter = "";

            var fieldTypes = GetCollection(collectionName).FieldTypes;

            foreach ( var keyColumn in keyFields )
            {
                var v = getValue( Rename(keyColumn.Replace( ".", " " ), realToDisplayNameMap) );
                var val = v == null
                    ? "null"
                    : $"\"{v}\"";

                if ( fieldTypes.TryGetValue( keyColumn, out var desc ) )
                {
                    if ( desc.Type == typeof(DateTime) && v?.GetType() == typeof(DateTime) )
                        val = isSql ? $"'{v:yyyy-MM-dd}'" : $"ISODate(\"{v:yyyy-MM-ddTHH:mm:ss.fff}Z\")";
                    else if ( desc.Type != typeof(string) )
                        val = val.Trim('\"');
                }

                if ( keyFilter != "" )
                    keyFilter += GetFilterConcatenationString(collectionName);
                    
                //keyFilter += $"{{ \"{keyColumn}\": {val} }}";
                keyFilter += await dataSource.GetDrilldownAsync(collectionName, keyColumn, val, true );
            }

            var nullValue = isString
                ? "\"\""
                : isDate
                    ? "null"
                    : "0.0";

            if ( keyFilter != "" )
                keyFilter += GetFilterConcatenationString(collectionName);

            keyFilter               += await dataSource.GetDrilldownAsync(collectionName, realName, nullValue );
            current.DrilldownFilter =  keyFilter;

            var keys1 = GetCollection(collectionName).GetDrilldownKeyFields(PivotFieldPurpose.PrimaryKey1);
            var keys2 = GetCollection(collectionName).GetDrilldownKeyFields(PivotFieldPurpose.PrimaryKey2);

            var keys = (keys1.All(x => keyFields.Contains(x)) ? keys2 : keys1 )
                      .Concat(
                           ShowAllKeysInDrilldowns
                               ? allKeys
                               : keyFields )
                      .Distinct()
                      .Where( allKeys.Contains )
                ;

            current.KeyFields  = keys.ToArray();
            current.DataFields = dataFields;

            _log.Debug( $"Drilldown Pivot=\"{source.Name}\" Filter:\n{current.Filter} DrilldownFilter:\n{current.DrilldownFilter}");

            return Tuple.Create(collectionName, current);
        }
        catch ( Exception  ex )
        {
            await ShowException( ex );
            return null;
        }
    }

    private static string GetFilterConcatenationString(string collectionName) => collectionName.StartsWith("Forge:") ? "," : "\n\tAND ";

    private async Task<Tuple<string, PivotDefinition>?> TryCustomDrilldown(
        IPivotTableDataSource dataSource,
        string                collectionName,
        PivotDefinition?      origPivot,
        Func<string, object?> getValue,
        string                column,
        string[]              allHeaders)
    {
        // try to find custom drilldown

        var rec = origPivot?.Drilldown?.FirstOrDefault( x => x.ColumnName == column )
             ?? origPivot?.Drilldown?.FirstOrDefault( x => x.ColumnName == DefaultDrilldownField )
            ;
        if ( rec == null )
            return null;

        // detect other keys shown
        var keyHeaders = GetCollection(collectionName).KeyFields;
        var keys = allHeaders
                  .Where( x => keyHeaders.Contains( x ) )
                  .Select( x => new KeyValuePair<string, string>(x, getValue( x )?.ToString() ?? "") )
            ;

        var basePivot = GetPivotDefinition( rec.DrilldownPivot, collectionName );

        if ( basePivot != null )
        {
            var pivot = await SameCollectionDrilldown( dataSource, collectionName, origPivot, getValue, column, allHeaders, basePivot, rec, keys );
            return Tuple.Create(collectionName, pivot);
        }

        var path = rec.DrilldownPivot.Split( '/' );
        if ( path.Length == 2 )
        {
            if ( !await MessageBoxShowYesNo(
                    $"Do you want to start separate instance and run \"{path[1]}\" for collection \"{path[0]}\" in order to drill down to \"{column}\"?",
                    "Drill down" ) )
            {
                return null;
            }

            var tuple = await SeparateCollectionDrilldown( origPivot, getValue, column, allHeaders, keys, path[0], path[1] );
            return tuple;
        }

        await MessageBoxShow( $"DrilldownPivot=\"{rec.DrilldownPivot}\" is not found. Column=\"{column}\"" );
        return null;
    }

    private async Task<Tuple<string, PivotDefinition>?> SeparateCollectionDrilldown(
        PivotDefinition?                          orig,
        Func<string, object?>                     getValue,
        string                                    column,
        string []                                 allHeaders,
        IEnumerable<KeyValuePair<string, string>> keys,
        string                                    destCollection,
        string                                    destPivotName
    )
    {
        var destPivot = GetPivotDefinition(destPivotName, destCollection)?.Clone();

        var rec = orig?.Drilldown.FirstOrDefault( x => x.ColumnName == column )
             ?? orig?.Drilldown.FirstOrDefault( x => x.ColumnName == DefaultDrilldownField )
            ;

        if (rec == null || destPivot == null)
        {
            await MessageBoxShow($"Drilldown is not configured for \"{column}\"");
            return null;
        }

        destPivot.DrilldownFilter = string.Concat(keys.Select(x => $"{{ \"{x.Key}\" : \"{x.Value}\" }}, ")) // other keys
          + PrepareDrilldownCondition(allHeaders, rec.DrilldownCondition, column, getValue); // drilldown condition

        return Tuple.Create(destCollection, destPivot);
    }

    private Task<PivotDefinition> SameCollectionDrilldown(
        IPivotTableDataSource                     dataSource,
        string                                    collectionName,
        PivotDefinition?                          origPivot,
        Func<string, object?>                     getValue,
        string                                    column,
        string []                                 allHeaders,
        PivotDefinition                           basePivot,
        PivotDefinition.DrilldownDef              rec,
        IEnumerable<KeyValuePair<string, string>> keys 
    )
    {
        var drill = basePivot.Clone();

        drill.DrilldownFilter = string.Concat(keys.Select(x => $"{{ \"{x.Key}\" : \"{x.Value}\" }}, ")) // other keys
          + PrepareDrilldownCondition(allHeaders, rec.DrilldownCondition, column, getValue); // drilldown condition

        drill.Name  = PivotDefinition.CurrentPivotName;
        drill.Group = PivotDefinition.CurrentPivotGroup;

        if ( !string.IsNullOrWhiteSpace( rec.AppendToBeforeGrouping ) )
        {
            // append to "Before Grouping"
            if ( !string.IsNullOrWhiteSpace( drill.BeforeGrouping ) )
                drill.BeforeGrouping += ",\n";
            drill.BeforeGrouping += PrepareDrilldownCondition( allHeaders, rec.AppendToBeforeGrouping, column, getValue );
        }

        var primaryKeys1 = GetCollection(collectionName).FieldTypes.Values
                                     .Where( x => x.Purpose == PivotFieldPurpose.PrimaryKey1 )
                                     .Select( x => x.Name )
                                     .ToList();

        if ( origPivot?.KeyFields.Intersect( primaryKeys1 ).Count() != primaryKeys1.Count
           ) // not all PK1 shown => add missing PK1
        {
            drill.KeyFields = drill.KeyFields.Concat( primaryKeys1.Where( x => !drill.KeyFields.Contains( x ) ) ).ToArray();
        }
        else
        {
            // do the same for PK2
            var primaryKeys2 = GetCollection(collectionName).FieldTypes.Values
                                         .Where( x => x.Purpose == PivotFieldPurpose.PrimaryKey2 )
                                         .Select( x => x.Name )
                                         .ToList();

            if ( origPivot.KeyFields.Intersect( primaryKeys2 ).Count() != primaryKeys2.Count
               ) // not all PK2 selected => add missing PK1 and PK2
            {
                drill.KeyFields = drill.KeyFields.Concat( primaryKeys1.Where( x => !drill.KeyFields.Contains( x ) ) )
                                       .ToArray();
                drill.KeyFields = drill.KeyFields.Concat( primaryKeys2.Where( x => !drill.KeyFields.Contains( x ) ) )
                                       .ToArray();
            }
        }

        return Task.FromResult(drill);
    }


    private IEnumerable<KeyValuePair<string, object?>> GetPrimaryKey2KeyFields(
        string                collectionName, 
        Func<string, object?> getValue,
        string []             allHeaders
    ) =>
        GetCollection(collectionName).FieldTypes
            .Where( x => x.Value.Purpose == PivotFieldPurpose.PrimaryKey2 )
            .Select( x => x.Key )
            .Where( allHeaders.Contains )
            .Select( key => new KeyValuePair<string, object?>( key, getValue( key ) ) );

    private static string PrepareDrilldownCondition( 
        string []             allHeaders, 
        string                cond, 
        string                columnName, 
        Func<string, object?> getValue 
    )
    {
        cond = allHeaders
           .Aggregate(
                cond, 
                (current, header) => current.Replace($"<{header}>", getValue(header)?.ToString())
            );

        cond = cond.Replace("<COLNAME>", columnName);
        return cond;
    }
}