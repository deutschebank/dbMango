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

public class LogRecordModel
{

    public DateTime      Time         { get; set; } 
    public string        Severity     { get; set; }  = string.Empty;
    public string        Category     { get; set; }  = string.Empty;
    public long          Id           { get; set; } 
    public string        Svc          { get; set; }  = string.Empty;
    public string        Ctx          { get; set; }  = string.Empty;
    public string        Msg          { get; set; }  = string.Empty;
    public BsonDocument? Attr         { get; set; }

    public static LogRecordModel FromBson(BsonDocument doc) =>
        new ()
        {
            Time     = doc.Contains("t")    ? doc["t"].ToUniversalTime() : default,
            Severity = doc.Contains("s")    ? doc["s"].AsString          : string.Empty,
            Category = doc.Contains("c")    ? doc["c"].AsString          : string.Empty,
            Id       = doc.Contains("id")   ? doc["id"].ToInt64()        : 0L,
            Svc      = doc.Contains("svc")  ? doc["svc"].AsString        : string.Empty,
            Ctx      = doc.Contains("ctx")  ? doc["ctx"].AsString        : string.Empty,
            Msg      = doc.Contains("msg")  ? doc["msg"].AsString        : string.Empty,
            Attr     = doc.Contains("attr") ? doc["attr"].AsBsonDocument : null
        };
}