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
using System.Runtime.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Rms.Risk.Mango.Pivot.Core;

internal class ArrayBasedPivotDataSerializer : SerializerBase<ArrayBasedPivotData>
{
    public static readonly DateTime UnixEpoch = new(
        1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static ulong ToMillisecondsSinceUnixEpoch(DateTime dateTimeUtc) => (ulong)(dateTimeUtc - UnixEpoch).TotalMilliseconds;

    public override void Serialize( BsonSerializationContext context, BsonSerializationArgs args, ArrayBasedPivotData value )
    {
        if ( value == null )
            throw new ArgumentNullException( nameof(value), "ArrayBasedPivotData instance is null" );
        if ( string.IsNullOrWhiteSpace( value.Id ) )
            throw new SerializationException( "Id is not set for ArrayBasedPivotData instance" );
        var headers = value.Headers;
        if ( headers.Count == 0 )
            throw new SerializationException("Headers count is zero for ArrayBasedPivotData instance");


        context.Writer.WriteStartDocument();
        context.Writer.WriteName("_id");
        context.Writer.WriteString(value.Id);
        context.Writer.WriteName("ExpireAt");
        context.Writer.WriteDateTime((long)ToMillisecondsSinceUnixEpoch(value.ExpireAt));

        context.Writer.WriteName("Headers");

        context.Writer.WriteStartArray();
        foreach ( var header in headers )
            context.Writer.WriteString( header );
        context.Writer.WriteEndArray();

        context.Writer.WriteName( "data" );
        context.Writer.WriteStartArray();

        for ( var row = 0; row < value.Count; row ++ )
        {
            context.Writer.WriteStartArray();

            for ( var col = 0; col < headers.Count; col++ )
            {
                var data = value.Get( col, row );
                if ( data == null )
                {
                    context.Writer.WriteNull();
                    continue;
                }

                var t = data.GetType();
                if (t == typeof(double))
                    context.Writer.WriteDouble((double)data);
                else if (t == typeof(string))
                    context.Writer.WriteString((string)data);
                else if (t == typeof(int))
                    context.Writer.WriteInt32((int)data);
                else if (t == typeof(long))
                    context.Writer.WriteInt64((long)data);
                else if (t == typeof(DateTime))
                    context.Writer.WriteDateTime((long)ToMillisecondsSinceUnixEpoch((DateTime)data));
                else if (t == typeof(bool))
                    context.Writer.WriteBoolean((bool)data);
                else
                    context.Writer.WriteNull();
            }
            context.Writer.WriteEndArray();
        }

        context.Writer.WriteEndArray();
        context.Writer.WriteEndDocument();
    }

    public override ArrayBasedPivotData Deserialize( BsonDeserializationContext context, BsonDeserializationArgs args )
    {
        context.Reader.ReadStartDocument();

        ExpectName(context, "_id");
        var id = context.Reader.ReadString();
        ExpectName(context, "ExpireAt");
        context.Reader.ReadDateTime();

        ExpectName(context, "Headers");

        context.Reader.ReadStartArray();
        var headers = new List<string>();
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
            headers.Add( context.Reader.ReadString() );
        context.Reader.ReadEndArray();

        var data = new ArrayBasedPivotData( headers ) { Id = id };

        ExpectName(context, "data");

        context.Reader.ReadStartArray();

        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument) // rows
        {
            context.Reader.ReadStartArray();

            var val = new object[headers.Count];
            var col = 0;
            while (context.Reader.ReadBsonType() != BsonType.EndOfDocument) // columns
            {
                var v = BsonValueSerializer.Instance.Deserialize(context);

                try
                {
                    val[col] = BsonTypeMapper.MapToDotNetValue(v);
                }
                catch ( Exception )
                {
                    v[col] = null;
                }

                col += 1;
            }
            context.Reader.ReadEndArray();

            data.Add( val );
        }

        context.Reader.ReadEndArray();
        context.Reader.ReadEndDocument();
        return data;
    }

    private static void ExpectName( BsonDeserializationContext context, string elementExpected )
    {
        var name = context.Reader.ReadName();
        if ( name != elementExpected )
            throw new SerializationException( $"{elementExpected} expected, but got {name}" );
    }
}