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
﻿using MongoDB.Bson;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class IndexesInfoModel
{
    public List<IndexInfoModel> Indexes { get; set; } = new();

    public static IndexesInfoModel FromBson(BsonDocument bsonDocument)
    {
        var model = new IndexesInfoModel();
        var indexesArray = bsonDocument["cursor"]["firstBatch"].AsBsonArray;

        foreach (var indexElement in indexesArray)
        {
            var indexDoc = indexElement.AsBsonDocument;
            var index = new IndexInfoModel
            {
                Version = indexDoc.Contains("v") ? indexDoc["v"].AsInt32 : 0,
                Key = indexDoc.Contains("key") && indexDoc["key"].IsBsonDocument
                    ? indexDoc["key"].AsBsonDocument.ToDictionary(k => k.Name, v => v.Value.ToString() ?? "")
                    : new(),
                Name = indexDoc.Contains("name") ? indexDoc["name"].AsString : string.Empty,
                ExpireAfterSeconds = indexDoc.Contains("expireAfterSeconds") ? indexDoc["expireAfterSeconds"].ToNullableInt32() : null
            };
            model.Indexes.Add(index);
        }

        return model;
    }

    public void CopyFrom(IndexesInfoModel source)
    {
        Indexes = source.Indexes.Select(index => index.Clone()).ToList();
    }

    public IndexesInfoModel Clone()
    {
        var clone = new IndexesInfoModel();
        clone.CopyFrom(this);
        return clone;
    }
}

public class IndexInfoModel
{
    public int Version                    { get; set; }
    public Dictionary<string, string> Key { get; set; } = new();
    public string Name                    { get; set; } = string.Empty;
    public int? ExpireAfterSeconds        { get; set; }

    public void CopyFrom(IndexInfoModel source)
    {
        Version            = source.Version;
        Key                = new(source.Key);
        Name               = source.Name;
        ExpireAfterSeconds = source.ExpireAfterSeconds;
    }

    public IndexInfoModel Clone()
    {
        var clone = new IndexInfoModel();
        clone.CopyFrom(this);
        return clone;
    }
}

public static class BsonExtensions
{
    public static int? ToNullableInt32(this BsonValue value)
    {
        return value.IsBsonNull ? (int?)null : value.AsInt32;
    }
}
