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
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Rms.Risk.Mango.Pivot.Core.Models;

namespace Rms.Risk.Mango.Pivot.Core;

public enum PivotTypeEnum
{
    Unknown           = 0,
    SimpleAggregation = 1,
//  MapReduce         = 2, // Obsolete
    CustomQuery       = 3,
    AggregationForHumans = 4
}


[BsonIgnoreExtraElements]
public class PivotDefinition : ICloneable, IComparable<PivotDefinition>, IComparable
{
    public const string UserPivotsGroup       = "User pivots";
    public const string PredefinedPivotsGroup = "Predefined";
    public const string CurrentPivotGroup     = "";
    public const string CurrentPivotName      = "<Current>";

    public static Func<PivotDefinition, FilterExpressionTree.ExpressionGroup? /*extra filter*/, Dictionary<string, Type> /*fieldTypes = null*/, Func<string,string> /*getAggregationOperator*/, string> ConvertToJson { get; set; } 
        = (pivot, extraFilter, fieldTypes, getAggOperator) => pivot.DefaultConvertToJson(extraFilter, fieldTypes, getAggOperator);

    public override string ToString() => $"{Name}: {ToJson(null, [], _ => "$sum")}";

    [BsonIgnore, JsonIgnore] public bool IsPredefined => !string.Equals(Group, UserPivotsGroup);

    object ICloneable.Clone() => Clone();

    public PivotDefinition Clone()
    {
        var o = (PivotDefinition)MemberwiseClone();

        o.KeyFields  = (string[])KeyFields.Clone();
        o.DataFields = (string[])DataFields.Clone();
        o.Drilldown = Drilldown
                        .Select( 
                             x => new DrilldownDef
                             {
                                 ColumnName             = x.ColumnName,
                                 DrilldownCondition     = x.DrilldownCondition,
                                 AppendToBeforeGrouping = x.AppendToBeforeGrouping,
                                 DrilldownPivot         = x.DrilldownPivot
                             } )
                        .ToList();
        if (LineChartDataSetKeys != null)
            o.LineChartDataSetKeys = [..LineChartDataSetKeys];

        return o;
    }

    public string GetFilterExpression(FilterExpressionTree.ExpressionGroup? extraFilter, Dictionary<string, Type> fieldTypes)
    {
        var filters = new []
            {
                Filter.Trim(),
                DrilldownFilter.Trim(),
                extraFilter?.ToJson(fieldTypes)
            }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .OfType<string>()
            .ToList();

        return filters.Count switch
        {
            0 => "",
            1 => filters[0],
            _ => $"{{ \"$and\" : [ {string.Join(" ", filters)} ] }}"
        };
    }

    public string ToJson(
        FilterExpressionTree.ExpressionGroup? extraFilter, 
        Dictionary<string, Type> fieldTypes,
        Func<string,string> getAggregationOperator
        )
        => ConvertToJson(this, extraFilter, fieldTypes, getAggregationOperator);

    private string DefaultConvertToJson(FilterExpressionTree.ExpressionGroup? extraFilter, Dictionary<string, Type> fieldTypes, Func<string,string>? getAggregationOperator = null )
    {
        switch ( PivotType )
        {
            default:
                return "";
            case PivotTypeEnum.SimpleAggregation:
                return SimpleAggregationToJson( extraFilter, fieldTypes, getAggregationOperator );
            case PivotTypeEnum.CustomQuery:
                return CustomQueryToJson( extraFilter, fieldTypes );
            case PivotTypeEnum.AggregationForHumans:
                return AggregationForHumansToJson( extraFilter, fieldTypes );
        }
    }

    private string AggregationForHumansToJson(FilterExpressionTree.ExpressionGroup? extraFilter, Dictionary<string, Type>? fieldTypes = null)
    {
        // It is actually implemented!
        // See AfhHelpers.ConvertToJson for details
        throw new NotSupportedException();
    }

    private string CustomQueryToJson( FilterExpressionTree.ExpressionGroup? extraFilter, Dictionary<string, Type> fieldTypes )
    {
        var keys             = string.Join( ",", KeyFields.Select( x => $"\"{x.Replace( ".", " " )}\" : \"${x}\"" ) );
        var preserveKeys     = string.Join( ",", KeyFields.Select( x => $"\"{x.Replace( ".", " " )}\" : 1" ) );
        var keysFromIdToRoot = string.Join( ",", KeyFields.Select( x => $"\"{x.Replace( ".", " " )}\" : \"$_id.{x}\"" ) );
        var json = CustomQuery
                  .Replace( "[KEYS]",                 keys )
                  .Replace( "[KEYS_PRESERVE]",        preserveKeys )
                  .Replace( "[KEYS_FROM_ID_TO_ROOT]", keysFromIdToRoot )
                  .Replace( "[EXTRA_FILTER]",         GetFilterExpression(extraFilter, fieldTypes))
            ;
        return json;
    }

    private string SimpleAggregationToJson(FilterExpressionTree.ExpressionGroup? extraFilter, Dictionary<string, Type> fieldTypes, Func<string, string>? getAggregationOperator )
    {
        getAggregationOperator ??= _ => "$sum";

        var keys = string.Join( ",", KeyFields.Select( x => $"\"{x.Replace( ".", " " )}\" : \"${x}\"" ) );
        var data = string.Join(
            ",",
            DataFields.Select( x => $"\"{x.Replace( ".", " " )}\" : {{ \"{getAggregationOperator( x )}\" : \"${x}\" }}" ) );

        var group =
            $"{{ $group : {{ _id : {{ {keys} }}," +
            data +
            (string.IsNullOrWhiteSpace( WithinGrouping ) ? "\n" : $",\n{WithinGrouping}\n") +
            "}},\n";

        var filter = GetFilterExpression(extraFilter, fieldTypes);
        var match  = "";
        if (!string.IsNullOrWhiteSpace(filter))
            match = $"{{ $match : {filter} }},\n";

        var bg = "";
        if ( !string.IsNullOrWhiteSpace( BeforeGrouping ) )
            bg = BeforeGrouping + ",\n";

        var ag = "";
        if ( !string.IsNullOrWhiteSpace( AfterGrouping ) )
            ag = AfterGrouping + ",\n";


        var json = "[\n" +
            match +
            bg +
            group +
            ag +
            "{ $sort : { _id : 1 } }\n" +
            "]";

        return FormatArray( json );
    }

    private static string FormatArray(string s)
    {
        try
        {
            var doc = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonArray>(s);
            return doc.ToJson( new() {Indent = true, OutputMode = JsonOutputMode.RelaxedExtendedJson} );
        }
        catch
        {
            return s;
        }
    }

    [Obsolete]
    public bool IsMapReduce { get; set; }
        
    [JsonConverter( typeof(StringEnumConverter))]
    [BsonRepresentation( BsonType.String)]
    public PivotTypeEnum PivotType { get; set; }

    public string Name  { get; set; } = "New pivot";
    public string Group { get; set; } = UserPivotsGroup;
    public bool   UserVisible { get; set; } = true;

    public string Owner { get; set; } = string.Empty;

    // aggregation

    public string[] KeyFields       { get; set; } = [];
    public string[] DataFields      { get; set; } = [];
    public string   Filter          { get; set; } = "";
    public string   DrilldownFilter { get; set; } = ""; // This should never be saved in metadata!
    public string   BeforeGrouping  { get; set; } = "";
    public string   WithinGrouping  { get; set; } = "";
    public string   AfterGrouping   { get; set; } = "";

    // map/reduce
    public string MapFunction         { get; set; } = "";
    public string ReduceFunction      { get; set; } = "";
    public string PostProcessFunction { get; set; } = "";

    // custom query
    public string CustomQuery { get; set; } = "";

    [BsonIgnoreExtraElements]
    public class DrilldownDef
    {
        /// <summary>
        ///  Column to drill down to or "" for default condition.
        /// </summary>
        public string ColumnName { get; set; } = "";
        /// <summary>
        /// Drilldown formulas. Each formula must end with comma!
        /// </summary>
        /// <remarks>
        /// Key is column name or empty string for default condition.
        /// The value is comma-separated list of conditions.
        /// Example:
        ///     { "RhoDetails.OpeningRho.AUD_CROSSCURRENCY.Data.3M" : { "$ne": 0.0 } },
        /// You can use "variables":
        ///     "&lt;column_name&gt;" - replace this with column value
        ///     "&lt;COLNAME&gt;" - replace this with column name
        /// Example:
        ///     { "RhoDetails.&lt;COLNAME&gt;.&lt;CCY&gt;_&lt;Curve&gt;.Data.&lt;Tenor&gt;" : { "$ne": 0.0 } },
        /// </remarks>
        public string DrilldownCondition { get; set; } = "";
        /// <summary>
        /// Append this to "Before Grouping" part of the query. You can use all "addFields" contents here.
        /// </summary>
        public string AppendToBeforeGrouping { get; set; } = "";
        /// <summary>
        /// Use this pivot def as drilldown report (conditions will be applied)
        /// </summary>
        public string DrilldownPivot { get; set; } = "";
    }

    public List<DrilldownDef> Drilldown { get; set; } = [];

    // options

    public Highlighting Highlighting   { get; set; } = new();
    public bool         AllowDiskUsage { get; set; }

    public List<string> ColumnsOrder { get; set; } = [];

    [BsonIgnore]
    public string ColumnsOrderText
    {
        get => string.Join("\n", ColumnsOrder);
        set
        {
            ColumnsOrder.Clear();
            ColumnsOrder.AddRange(value?.Replace("\r", "").Split('\n').Where(x => !string.IsNullOrWhiteSpace( x )) ?? Array.Empty<string>());
        }
    }

    /// <summary>
    /// Column renaming map. Key is a real column name / value is display name.
    /// </summary>
    public Dictionary<string,string> RenameColumn { get; set; } = [];

    public bool         Make2DPivot           { get; set; }
    public List<string> Pivot2DRows           { get; set; } = [];
    public string       Pivot2DColumn         { get; set; } = "";
    public string       Pivot2DData           { get; set; } = "";
    public string       Pivot2DDataTypeColumn { get; set; } = "";

    public bool          MakeLineChart        { get; set; }
    public string?       LineChartXAxis       { get; set; }
    public List<string>? LineChartDataSetKeys { get; set; }
    public List<string>? LineChartYAxis       { get; set; }
    public bool          LineChartShowLegend  { get; set; }
    public bool          LineChartSteppedLine { get; set; }
    public bool          LineChartFill        { get; set; }

    public bool   ShowTotals          { get; set; } = true;
    public double HighlightTopPercent { get; set; } = 10.0;

    int IComparable.CompareTo(object? obj) => CompareTo(obj as PivotDefinition);

    public int CompareTo(PivotDefinition? other)
    {
        if (ReferenceEquals(this, other)) 
            return 0;
        if (ReferenceEquals(null, other))
            return 1;
        var groupComparison = string.Compare(Group, other.Group, StringComparison.Ordinal);
        if (groupComparison != 0) 
            return groupComparison;

        return string.Compare(Name, other.Name, StringComparison.Ordinal);
    }
}

[BsonIgnoreExtraElements]
public class PivotDefinitions
{
    [BsonId] public string                Id           { get; set; } = "";
    public          List<PivotDefinition> Pivots       { get; set; } = [];
}