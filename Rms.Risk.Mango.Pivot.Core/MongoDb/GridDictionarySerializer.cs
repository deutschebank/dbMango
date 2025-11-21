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
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public class GridDictionarySerializer<T> : DictionarySerializerBase<Dictionary<Tuple<string, string>, T>>
{
    public GridDictionarySerializer()
        : base(DictionaryRepresentation.Document, new RiskGridKeySerializer(), BsonSerializer.LookupSerializer<T>() )
    {
    }
    public GridDictionarySerializer(IBsonSerializer<T> valueSerializer)
        : base(DictionaryRepresentation.Document, new RiskGridKeySerializer(), valueSerializer)
    {
    }
    protected override Dictionary<Tuple<string, string>, T> CreateInstance() => [];
}