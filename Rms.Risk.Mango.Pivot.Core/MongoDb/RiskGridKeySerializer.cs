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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

internal class RiskGridKeySerializer : ClassSerializerBase<Tuple<string, string>>
{
    public override Tuple<string, string> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;
        var bsonType   = bsonReader.GetCurrentBsonType();

        switch (bsonType)
        {
            case BsonType.String:
            {
                var s = bsonReader.ReadString();
                if (string.IsNullOrWhiteSpace(s))
                    throw CreateCannotBeDeserializedException();
                        
                var ss = s.Split('_');

                if (ss.Length < 2)
                    throw CreateCannotBeDeserializedException();

                if ( ss.Length > 2 ) // if instrument contained '_' let's re-join it
                    ss[1] = string.Join( "_", ss.Skip( 1 ) );

                return Tuple.Create(string.Intern(ss[0]), string.Intern(ss[1]));
            }

            default:
                throw CreateCannotDeserializeFromBsonTypeException(bsonType);
        }
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Tuple<string, string> value) =>
        // Format: tenor_instrument
        context.Writer.WriteString($"{value.Item1}_{value.Item2}");
}