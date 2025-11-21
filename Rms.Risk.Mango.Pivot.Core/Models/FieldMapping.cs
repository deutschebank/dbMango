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

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class FieldMapping
{
    public const double DrilldownTolerance = 0.000001;

    public FieldMappingData Data { get; set; } = new();

    public int Count                                                                     => Data.Fields.Count;
    public bool UseMapping                                                               => Data.UseMapping;
    public IEnumerable<string> FieldNames                                                => Data.Fields.Keys;
    public IEnumerable<KeyValuePair<string, SingleFieldMapping>> Fields => Data.Fields;
    public IEnumerable<string> CalculatedFields                                          => Data.CalculatedFields.Keys;

    public override string ToString() => $"Fields={Count} CalculatedFields={Data.CalculatedFields.Count} Lookups={Data.Lookups.Count}";

    public bool TryGetValue( string name, out SingleFieldMapping? mapping ) => Data.Fields.TryGetValue( name, out mapping );

    public SingleFieldMapping this[ string name ]
    {
        get
        {
            if ( !Data.Fields.TryGetValue( name,                    out var mapping )
             && !Data.Fields.TryGetValue( name.Replace( ".", " " ), out mapping ) )
                throw new MissingFieldException( $"Field \"{name}\" is not found" );

            return mapping;
        }

        set => Data.Fields[name] = value;
    }

    public bool ContainsKey( string name ) =>
        Data.Fields.ContainsKey( name ) 
        || Data.Fields.ContainsKey( name.Replace( ".", " " ) );

    public string MapField(string name)
    {
        if (!Data.UseMapping)
            return name;

        return !TryGetValue(name, out var m)
            ? name
            : $"f{m?.Id}";
    }

    public string UnmapField(string name)
    {
        if (!Data.UseMapping || !name.StartsWith("f"))
            return name;
        if (!int.TryParse(name[1..], out var id) || id <= 0)
            return name;
        var n = Data.Fields.FirstOrDefault(x => x.Value.Id == id).Key;
        return n ?? name;
    }

    public FieldMapping(bool use)
    {
        Data.UseMapping = use;
    }

    public void MapAllFields(IList<BsonDocument> pipeline)
    {
        for (var i = 0; i < pipeline.Count; i++)
        {
            var stageName = pipeline[i].Elements.First().Name;
            pipeline[i] = MapAllFields(pipeline[i], stageName == "$project" || stageName == "$match");
        }
    }

    public BsonDocument MapAllFields(BsonDocument bsonDocument, bool replaceNakedNames)
    {
        var json = bsonDocument.ToJson();

        json = MapAllFields(json, replaceNakedNames);

        return BsonDocument.Parse(json);
    }

    public string MapAllFields(string json, bool replaceNakedNames)
    {
        // ordering by key.Length to resolve conflicts like "TradePV" vs "PV"

        json = Data.CalculatedFields.OrderBy(x => -x.Key.Length).Aggregate(json, (current, m) => current.Replace($"\"${m.Key}\"", m.Value.Formula));
        if (Data.UseMapping)
            json = Data.Fields.OrderBy(x => -x.Key.Length).Aggregate(json, (current, m) => current.Replace($"\"${m.Key}\"", $"\"$f{m.Value.Id}\""));
        if ( !replaceNakedNames )
            return json;

        json = Data.CalculatedFields.OrderBy(x => -x.Key.Length).Aggregate(json, (current, m) => current.Replace($"\"{m.Key}\"", m.Value.Formula));
        if (Data.UseMapping)
            json = Data.Fields.OrderBy(x => -x.Key.Length).Aggregate(json, (current, m) => current.Replace($"\"{m.Key}\"", $"\"f{m.Value.Id}\""));

        return json;
    }

    public void ClearCalculatedFields() => Data.CalculatedFields.Clear();

    public void AddCalculatedField(string name, string formula, string drillDown, string []? lookupDef = null, string aggregationOperator = "$sum") => Data.CalculatedFields[name] = new(formula, drillDown, lookupDef, aggregationOperator);

    public bool IsCalculated(string name) => Data.CalculatedFields.ContainsKey(name);

    public string GetDrilldown(string column, string value = "\"\"", bool equals = false)
    {
        if ( Data.CalculatedFields.TryGetValue(column, out var field) )
            return field.DrillDown;

        Data.Fields.TryGetValue(column, out var mapping);
        var isDouble = mapping != null && (mapping.Type == typeof(double) || mapping.Type == typeof(float) || mapping.Type == typeof(decimal));

        var name = Data.Fields.ContainsKey( column.Replace(" ", ".") )
            ? column.Replace(" ", ".")
            : column;

        if (equals)
        {
            //TODO: this compares all double with ==. This may not be ideal, but I don't know how to implement abs(field -value) < TOLERANCE in $filter step
            return $"{{ \"{name}\": {value} }}"; // all the rest including integers and strings
        }

        return isDouble
            ? $"{{ \"$or\" : [ {{ \"{name}\": {{ \"$lte\" : {-DrilldownTolerance} }} }}, {{ \"{name}\": {{ \"$gte\" : {DrilldownTolerance} }} }} ] }}" // double = special
            : $"{{ \"{name}\": {{ \"$ne\" :  {value} }} }}"; // all the rest including integers and strings
    }

    public string GetLookup(string name)
    {
        var lookups = Data.CalculatedFields[name].LookupDef
                          .Where(Data.Lookups.ContainsKey)
                          .Distinct()
                          .Select(x => Data.Lookups[x])
            ;

        return IsCalculated( name )
                ? string.Join( ", " , lookups)
                : ""
            ;
    }

    public string GetFormula(string name) => Data.CalculatedFields[name].Formula;

    public string GetAggregationOperator(string name) =>
        IsCalculated(name)
            ? Data.CalculatedFields[name].AggregationOperator
            : "$sum";

    public void AddLookup( string elem, string stages ) => Data.Lookups[elem] = stages;
}