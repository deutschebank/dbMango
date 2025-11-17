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
﻿using Rms.Risk.Mango.Pivot.Core.MongoDb;
using NjiAttribute = Newtonsoft.Json.JsonIgnoreAttribute;
using TjiAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class PivotSettings
{
    public class ClickHouseSettings
    {

        public string ClickHouseUrl      { get; set; } = "";
        public string ClickHouseDatabase { get; set; } = "";
        public string ClickHouseUser     { get; set; } = "";
        public string ClickHousePassword { get; set; } = "";
    }

    public int      ReloadIntervalMin    { get; set; }
    public string[] PreloadedCollections { get; set; } = [];


    public MongoDbConfigRecord? MongoDb { get; set; }
    public ClickHouseSettings?  BFG     { get; set; }

    [Nji,Tji] public string ClickHouseUrl                   => BFG?.ClickHouseUrl      ?? "";
    [Nji,Tji] public string ClickHouseDatabase              => BFG?.ClickHouseDatabase ?? "";
    [Nji,Tji] public string ClickHouseUser                  => BFG?.ClickHouseUser     ?? "";
    [Nji,Tji] public string ClickHousePassword              => BFG?.ClickHousePassword ?? "";
}