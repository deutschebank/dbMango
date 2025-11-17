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
using MongoDB.Bson.Serialization.Attributes;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

[DataContract
, Serializable
]
public abstract class MongoDbDocumentBase : IMongoDbDocumentBase
{
    public override string ToString() => Id ?? GetType().Name;

    private string _id = "";

    /// <summary>
    /// Method used to build document id from fields.
    /// The method MUST return null in case of insufficient data. 
    /// Once value returned Id field will NOT be automatically updated on any changes to the data used to make Id
    /// </summary>
    /// <returns>Id string</returns>
    protected abstract string BuildId();    

    [BsonId]
    [DataMember]
    public string Id
    {
        get
        {
            if ( !string.IsNullOrWhiteSpace( _id ) )
                return _id;
            _id = BuildId();
            return _id;
        }
        set => _id = value;
    }
}