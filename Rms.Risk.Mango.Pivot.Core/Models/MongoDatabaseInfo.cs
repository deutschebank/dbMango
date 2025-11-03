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
﻿using System.Text.Json.Serialization;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class MongoDatabaseInfo
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";

    [JsonPropertyName( "sizeOnDisk")]
    public decimal SizeOnDisk { get; set; }

    [JsonPropertyName( "empty")]
    public bool Empty { get; set; }
}