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
using Newtonsoft.Json;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class UserInfoModel
{
    public string UserName  { get; set; } = string.Empty;
    public string Db        { get; set; } = string.Empty;
    [JsonIgnore]                    
    public bool   IsBuiltin { get; set; }
    [JsonIgnore]                    
    public string? Password          { get; set; }
    public List<RoleInDbModel> Roles { get; set; } = [];

    public static UserInfoModel FromBson(BsonDocument bsonDocument) =>
        new()
        {
            UserName  = bsonDocument.GetValue("user",  string.Empty).AsString,
            Db        = bsonDocument.GetValue("db", string.Empty).AsString,
            IsBuiltin = false,
            Password  = null,
            Roles = bsonDocument.Contains("roles")
                ? bsonDocument["roles"].AsBsonArray
                   .Select(role => new RoleInDbModel
                    {
                        Db   = role["db"].AsString,
                        Role = role["role"].AsString
                    }).ToList()
                : []
        };

    public void CopyFrom(UserInfoModel other)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));

        UserName  = other.UserName;
        Db        = other.Db;
        IsBuiltin = other.IsBuiltin;
        Password  = other.Password;
        Roles     = other.Roles.Select(role => new RoleInDbModel
        {
            Db    = role.Db,
            Role  = role.Role
        }).ToList();
    }

    public UserInfoModel Clone()
    {
        var clone = new UserInfoModel();
        clone.CopyFrom(this);
        return clone;
    }

}

public class UsersModel
{
    public List<UserInfoModel> Users { get; set; } = [];

    public static UsersModel FromBson(BsonDocument bsonDocument)
    {
        var model = new UsersModel();
        var usersArray = bsonDocument["users"].AsBsonArray;
        foreach (var userElement in usersArray)
        {
            var userDoc = userElement.AsBsonDocument;
            var user = UserInfoModel.FromBson(userDoc);
            model.Users.Add(user);
        }
        return model;
    }

    public void CopyFrom(UsersModel other)
    {
        Users = other.Users.Select(user => user.Clone()).ToList();
    }

    public UsersModel Clone()
    {
        var clone = new UsersModel();
        clone.CopyFrom(this);
        return clone;
    }
}
