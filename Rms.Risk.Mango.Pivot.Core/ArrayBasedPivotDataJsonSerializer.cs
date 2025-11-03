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
﻿using System.Runtime.Serialization;
#if MSJSON
using System.Text.Json;
using System.Text.Json.Serialization;
#else
using Newtonsoft.Json;
#endif
namespace Rms.Risk.Mango.Pivot.Core;
#if MSJSON
    public class ArrayBasedPivotDataJsonSerializer : JsonConverter<ArrayBasedPivotData>
    {
        public override void Write(
            Utf8JsonWriter writer,
            ArrayBasedPivotData value,
            JsonSerializerOptions options)
        {
            if ( string.IsNullOrWhiteSpace( value.Id ) )
                throw new SerializationException( "Id is not set for ArrayBasedPivotData instance" );
            var headers = value.Headers.OrderBy(x => x.Value).Select(x => x.Key).ToList();
            if ( headers.Count == 0 )
                throw new SerializationException("Headers count is zero for ArrayBasedPivotData instance");

            writer.WriteStartObject();

            writer.WritePropertyName("_id");
            writer.WriteStringValue(value.Id);
            writer.WritePropertyName("ExpireAt");
            writer.WriteStringValue(value.ExpireAt.ToString("O"));

            writer.WritePropertyName("Headers");

            writer.WriteStartArray();
            foreach ( var header in headers )
                writer.WriteStringValue( header );
            writer.WriteEndArray();

            writer.WritePropertyName( "Data" );
            writer.WriteStartArray();

            for ( var row = 0; row < value.Count; row ++ )
            {
                writer.WriteStartArray();

                for ( var col = 0; col < headers.Count; col++ )
                {
                    var data = value.Get( col, row );
                    if ( data == null )
                    {
                        writer.WriteNullValue();
                        continue;
                    }

                    var t = data.GetType();
                    if (t == typeof(double))
                        writer.WriteNumberValue((double)data);
                    else if (t == typeof(string))
                        writer.WriteStringValue((string)data);
                    else if (t == typeof(int))
                        writer.WriteNumberValue((int)data);
                    else if (t == typeof(long))
                        writer.WriteNumberValue((long)data);
                    else if (t == typeof(DateTime))
                        writer.WriteStringValue(((DateTime)data).ToString("O"));
                    else if (t == typeof(bool))
                        writer.WriteBooleanValue((bool)data);
                    else
                        writer.WriteNullValue();
                }
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static void Expect( Utf8JsonReader reader, JsonTokenType expected )
        {
            if ( !reader.Read() )
                throw new SerializationException();
            if ( reader.TokenType != expected )
                throw new SerializationException($"Expected {expected} but got {reader.TokenType}");
        }

        private static void ReadStartArray( Utf8JsonReader reader ) => Expect( reader, JsonTokenType.StartArray );
        private static void ReadEndProperty( Utf8JsonReader reader ) => Expect( reader, JsonTokenType.EndObject );

        private static void ReadProperty( Utf8JsonReader reader, string name )
        {
            Expect( reader, JsonTokenType.PropertyName );
            var n = reader.GetString();
            if ( n != name )
                throw new SerializationException($"Expected \"{name}\" but got \"{n}\"");
        }


        public override ArrayBasedPivotData Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            ReadProperty(reader, "_id");
            var id = reader.GetString();
            ReadProperty(reader, "ExpireAt");
            var expireAt = DateTime.Parse(reader.GetString());

            ReadProperty(reader, "Headers");

            ReadStartArray(reader);
            var headers = new List<string>();

            while ( reader.Read() )
            {
                var tokenType = reader.TokenType;
                if ( tokenType == JsonTokenType.EndArray )
                    break;
                headers.Add( reader.GetString() );
            }

            var data = new ArrayBasedPivotData( headers )
            {
                Id       = id, 
                ExpireAt = expireAt
            };

            ReadProperty(reader, "Data");

            while ( reader.Read() )
            {
                var tokenType = reader.TokenType;
                if ( tokenType == JsonTokenType.EndArray )
                    break;

                if ( tokenType != JsonTokenType.StartArray )
                    continue;

                var val = new object[headers.Count];
                var col = 0;

                while ( reader.Read() )
                {
                    tokenType = reader.TokenType;
                    if ( tokenType == JsonTokenType.StartArray )
                        continue;
                    if ( tokenType == JsonTokenType.EndArray )
                        break;

                    switch ( tokenType )
                    {
                        case JsonTokenType.Comment:
                        case JsonTokenType.None:
                            break;
                        // case JsonTokenType.Float:
                        // case JsonTokenType.Boolean:
                        // case JsonTokenType.Date:
                        // case JsonTokenType.Integer:
                        // case JsonTokenType.Bytes:
                        case JsonTokenType.String:
                            val[col] = reader.GetString();
                            break;
                        case JsonTokenType.Number:
                            var s = reader.GetString();
                            // since doubles are more than 90% of the data make it quick
                            if ( s.IndexOf(".", StringComparison.Ordinal) >= 0 && double.TryParse( s, out var d ) )
                                val[col] = d;
                            if ( int.TryParse( s, out var i ) )
                                val[col] = i;
                            if ( long.TryParse( s, out var l ) )
                                val[col] = l;
                            if ( double.TryParse( s, out d ) )
                                val[col] = d;
                            break;
                        case JsonTokenType.Null:
                            val[col] = null;
                            break;
                        // case JsonToken.StartObject:
                        // case JsonToken.StartArray:
                        // case JsonToken.StartConstructor:
                        // case JsonToken.PropertyName:
                        // case JsonToken.Raw:
                        // case JsonToken.Undefined:
                        // case JsonToken.EndObject:
                        // case JsonToken.EndConstructor:
                        default:
                            throw new SerializationException($"Unexpected token {tokenType}");
                    }

                    col += 1;
                }

                data.Add( val );
            }

            ReadEndProperty( reader );
            return data;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(ArrayBasedPivotData) == objectType;
        }
    }
#else
public class ArrayBasedPivotDataJsonSerializer : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? val, JsonSerializer serializer)
    {
        if ( val is not ArrayBasedPivotData value )
            throw new ArgumentNullException( nameof(value), "ArrayBasedPivotData instance is null" );
        if ( string.IsNullOrWhiteSpace( value.Id ) )
            throw new SerializationException( "Id is not set for ArrayBasedPivotData instance" );
        var headers = value.Headers;
        if ( headers.Count == 0 )
            throw new SerializationException("Headers count is zero for ArrayBasedPivotData instance");

        writer.WriteStartObject();

        writer.WritePropertyName("_id");
        writer.WriteValue(value.Id);
        writer.WritePropertyName("ExpireAt");
        writer.WriteValue(value.ExpireAt);

        writer.WritePropertyName("Headers");

        writer.WriteStartArray();
        foreach ( var header in headers )
            writer.WriteValue( header );
        writer.WriteEndArray();

        writer.WritePropertyName( "Data" );
        writer.WriteStartArray();

        for ( var row = 0; row < value.Count; row ++ )
        {
            writer.WriteStartArray();

            for ( var col = 0; col < headers.Count; col++ )
            {
                var data = value.Get( col, row );
                if ( data == null )
                {
                    writer.WriteNull();
                    continue;
                }

                var t = data.GetType();
                if (t == typeof(double))
                    writer.WriteValue((double)data);
                else if (t == typeof(string))
                    writer.WriteValue((string)data);
                else if (t == typeof(int))
                    writer.WriteValue((int)data);
                else if (t == typeof(long))
                    writer.WriteValue((long)data);
                else if (t == typeof(DateTime))
                    writer.WriteValue((DateTime)data);
                else if (t == typeof(bool))
                    writer.WriteValue((bool)data);
                else
                    writer.WriteNull();
            }
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Expect( JsonReader reader, JsonToken expected )
    {
        if ( !reader.Read() )
            throw new SerializationException($"Expected {expected} but got none");
        if ( reader.TokenType != expected )
            throw new SerializationException($"Expected {expected} but got {reader.TokenType}");
    }

    private static void ReadStartArray( JsonReader  reader ) => Expect( reader, JsonToken.StartArray );
    private static void ReadEndProperty( JsonReader reader ) => Expect( reader, JsonToken.EndObject );

    private static void ReadProperty( JsonReader reader, string name )
    {
        Expect( reader, JsonToken.PropertyName );
        if ( reader.ValueType != typeof(string))
            throw new SerializationException($"Expected {name} but got \"{reader.Value}\" ({reader.ValueType})");
        var n = reader.Value as string;
        if ( n != name )
            throw new SerializationException($"Expected \"{name}\" but got \"{n}\"");
    }


    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ReadProperty(reader, "_id");
        var id = reader.ReadAsString();
        ReadProperty(reader, "ExpireAt");
        var expireAt = reader.ReadAsDateTime();

        ReadProperty(reader, "Headers");

        ReadStartArray(reader);
        var headers = new List<string>();

        while ( reader.Read() )
        {
            var tokenType = reader.TokenType;
            if ( tokenType == JsonToken.EndArray )
                break;
            headers.Add( (string)reader.Value! );
        }

        var data = new ArrayBasedPivotData( headers )
        {
            Id       = id ?? "", 
            ExpireAt = expireAt ?? default(DateTime)
        };

        if (data.Id == "no_data")
            return data;

        ReadProperty(reader, "Data");

        while ( reader.Read() )
        {
            var tokenType = reader.TokenType;
            if ( tokenType == JsonToken.EndArray )
                break;

            if ( tokenType != JsonToken.StartArray )
                continue;

            var val = new object[headers.Count];
            var col = 0;

            while ( reader.Read() )
            {
                tokenType = reader.TokenType;
                if ( tokenType == JsonToken.StartArray )
                    continue;
                if ( tokenType == JsonToken.EndArray )
                    break;

                switch ( tokenType )
                {
                    case JsonToken.Comment:
                    case JsonToken.None:
                        break;
                    case JsonToken.Float:
                    case JsonToken.String:
                    case JsonToken.Boolean:
                    case JsonToken.Date:
                    case JsonToken.Null:
                    case JsonToken.Integer:
                    case JsonToken.Bytes:
                        val[col] = reader.Value!;
                        break;
                    // case JsonToken.StartObject:
                    // case JsonToken.StartArray:
                    // case JsonToken.StartConstructor:
                    // case JsonToken.PropertyName:
                    // case JsonToken.Raw:
                    // case JsonToken.Undefined:
                    // case JsonToken.EndObject:
                    // case JsonToken.EndConstructor:
                    default:
                        throw new SerializationException($"Unexpected token {tokenType}");
                }

                col += 1;
            }

            data.Add( val );
        }

        ReadEndProperty( reader );
        return data;
    }

    public override bool CanRead => true;

    public override bool CanConvert(Type objectType) => typeof(ArrayBasedPivotData) == objectType;
}

#endif