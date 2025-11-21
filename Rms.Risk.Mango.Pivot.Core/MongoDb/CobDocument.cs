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

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

[DataContract
, Serializable
]
public abstract class CobDocument : MongoDbDocumentBase, ICobDocument
{
    public override string ToString() => Id ?? GetType().Name;

    private string _layer = "";

    public static string MakeId(DateTime cob, string id, string? layer = null)
    {
        if (cob != default)
            return string.IsNullOrWhiteSpace(layer)
                    ? $"{cob:yyyyMMdd}-{id}"
                    : $"{cob:yyyyMMdd}-{id}-{layer}"
                ;
        return string.IsNullOrWhiteSpace(layer)
                ? $"{id}"
                : $"{id}-{layer}"
            ;
    }

    [DataMember] public DateTime ExpireAt { get; set; }
    [DataMember]
    public string Layer
    {
        get => _layer;
        set => _layer = string.IsNullOrWhiteSpace(value) ? "" : string.Intern(value);
    }
    [DataMember] public DateTime COB { get; set; }
}