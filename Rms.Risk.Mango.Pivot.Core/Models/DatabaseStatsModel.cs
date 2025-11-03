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

namespace Rms.Risk.Mango.Services.Models;

public class DatabaseStatsModel
{
    public        Dictionary<string, DatabaseStatsRaw> Raw { get; set; } = new();

    public static DatabaseStatsModel FromBson(BsonDocument res)
    {
        var model = new DatabaseStatsModel();

        // Check if "raw" exists and is a document
        if (res.Contains("raw") && res["raw"].IsBsonDocument)
        {
            var rawDoc = res["raw"].AsBsonDocument;
            foreach (var element in rawDoc.Elements)
            {
                // Each element's value is a BsonDocument representing DatabaseStatsRaw
                if (element.Value.IsBsonDocument)
                {
                    var statsRaw = DatabaseStatsRaw.FromBson(element.Value.AsBsonDocument);
                    model.Raw[element.Name] = statsRaw;
                }
            }
        }

        return model;
    }
}

public class DatabaseStatsRaw
{
    public static DatabaseStatsRaw FromBson(BsonDocument doc) =>
        new()
        {
            Db                   = doc.GetValue("db",                   "").AsString,
            Collections          = doc.GetValue("collections",          0).ToInt32(),
            Views                = doc.GetValue("views",                0).ToInt32(),
            Objects              = doc.GetValue("objects",              0L).ToInt64(),
            AvgObjSize           = doc.GetValue("avgObjSize",           0.0).ToDouble(),
            DataSize             = doc.GetValue("dataSize",             0.0).ToDouble(),
            StorageSize          = doc.GetValue("storageSize",          0.0).ToDouble(),
            FreeStorageSize      = doc.GetValue("freeStorageSize",      0.0).ToDouble(),
            Indexes              = doc.GetValue("indexes",              0).ToInt32(),
            IndexSize            = doc.GetValue("indexSize",            0.0).ToDouble(),
            IndexFreeStorageSize = doc.GetValue("indexFreeStorageSize", 0.0).ToDouble(),
            TotalSize            = doc.GetValue("totalSize",            0.0).ToDouble(),
            TotalFreeStorageSize = doc.GetValue("totalFreeStorageSize", 0.0).ToDouble(),
            ScaleFactor          = doc.GetValue("scaleFactor",          0).ToInt32(),
            FsUsedSize           = doc.GetValue("fsUsedSize",           0.0).ToDouble(),
            FsTotalSize          = doc.GetValue("fsTotalSize",          0.0).ToDouble(),
            Ok                   = doc.GetValue("ok",                   0.0).ToDouble()
        };

    public string Db { get; set; } = string.Empty;
    public int Collections { get; set; }
    public int Views { get; set; }
    public long Objects { get; set; }
    public double AvgObjSize { get; set; }
    public double DataSize { get; set; }
    public double StorageSize { get; set; }
    public double FreeStorageSize { get; set; }
    public int Indexes { get; set; }
    public double IndexSize { get; set; }
    public double IndexFreeStorageSize { get; set; }
    public double TotalSize { get; set; }
    public double TotalFreeStorageSize { get; set; }
    public int ScaleFactor { get; set; }
    public double FsUsedSize { get; set; }
    public double FsTotalSize { get; set; }
    public double Ok { get; set; }


}