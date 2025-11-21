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
using MongoDB.Bson.Serialization.Attributes;

namespace Rms.Risk.Mango.Pivot.Core.Models;

[BsonIgnoreExtraElements]
public class FieldMappingData : ICloneable
{
    public DateTime CachedAt   { get; set; }
    public bool     UseMapping { get; set; }

    public Dictionary<string, SingleFieldMapping> Fields           { get; set; } = new( StringComparer.OrdinalIgnoreCase );
    public Dictionary<string, CalcFieldDef>       CalculatedFields { get; set; } = new(       StringComparer.OrdinalIgnoreCase );
    public Dictionary<string, string>             Lookups          { get; set; } = new(             StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// Call after loading from Json/Bson to replace # in field names to .
    /// </summary>
    public void PostLoad()
    {
        var fields           = new Dictionary<string, SingleFieldMapping>( StringComparer.OrdinalIgnoreCase );
        var calculatedFields = new Dictionary<string, CalcFieldDef>(       StringComparer.OrdinalIgnoreCase );
        var lookups          = new Dictionary<string, string>(             StringComparer.OrdinalIgnoreCase );

        foreach ( var x in Fields )
            fields.Add( x.Key.Replace( "#", "." ), x.Value );
        foreach ( var x in CalculatedFields )
            calculatedFields.Add( x.Key.Replace( "#", "." ), x.Value );
        foreach ( var x in Lookups )
            lookups.Add( x.Key.Replace( "#", "." ), x.Value );

        Fields           = fields;
        CalculatedFields = calculatedFields;
        Lookups          = lookups;
    }

    /// <summary>
    /// Call before saving to Json/Bson to replace . in field names to #
    /// </summary>
    public void PreSave()
    {
        var now = DateTime.Now;
        CachedAt = new(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

        var fields           = new Dictionary<string, SingleFieldMapping>( StringComparer.OrdinalIgnoreCase );
        var calculatedFields = new Dictionary<string, CalcFieldDef>(       StringComparer.OrdinalIgnoreCase );
        var lookups          = new Dictionary<string, string>(             StringComparer.OrdinalIgnoreCase );

        foreach ( var x in Fields )
            fields.Add( x.Key.Replace( ".", "#" ), x.Value );
        foreach ( var x in CalculatedFields )
            calculatedFields.Add( x.Key.Replace( ".", "#" ), x.Value );
        foreach ( var x in Lookups )
            lookups.Add( x.Key.Replace( ".", "#" ), x.Value );

        Fields           = fields;
        CalculatedFields = calculatedFields;
        Lookups          = lookups;
    }

    object ICloneable.Clone() => Clone();

    public FieldMappingData Clone()
    {
        var d = new FieldMappingData
        {
            CachedAt   = CachedAt,
            UseMapping = UseMapping
        };

        foreach ( var x in Fields )
            d.Fields.Add( x.Key, x.Value.Clone() );
        foreach ( var x in CalculatedFields )
            d.CalculatedFields.Add( x.Key, x.Value.Clone() );
        foreach ( var x in Lookups )
            d.Lookups.Add( x.Key, x.Value );

        return d;
    }
}