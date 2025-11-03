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

public class RolesInfoModel
{
    public List<RoleInfoModel> Roles { get; set; } = [];

    public static RolesInfoModel FromBson(BsonDocument bsonDocument)
    {
        var model = new RolesInfoModel();
        var rolesArray = bsonDocument["roles"].AsBsonArray;

        foreach (var roleElement in rolesArray)
        {
            var roleDoc = roleElement.AsBsonDocument;
            var role = new RoleInfoModel
            {
                RoleName            = roleDoc.Contains("role")   ? roleDoc["role"].AsString : string.Empty,
                Db                  = roleDoc.Contains("db")     ? roleDoc["db"].AsString : string.Empty,
                IsBuiltin           = roleDoc.Contains("isBuiltin") && roleDoc["isBuiltin"].AsBoolean,
                Roles = roleDoc.Contains("roles") && roleDoc["roles"].IsBsonArray
                    ? roleDoc["roles"].AsBsonArray.Select(r =>
                    {
                        var roleInDbDoc = r.AsBsonDocument;
                        return new RoleInDbModel
                        {
                            Role = roleInDbDoc.Contains("role") ? roleInDbDoc["role"].AsString : string.Empty,
                            Db   = roleInDbDoc.Contains("db")   ? roleInDbDoc["db"].AsString   : string.Empty
                        };
                    }).ToList()
                    : [],
                InheritedRoles      = roleDoc.Contains("inheritedRoles") && roleDoc["inheritedRoles"].IsBsonArray 
                    ? roleDoc["inheritedRoles"].AsBsonArray.Select(r => r.ToString()!).ToList() 
                    : [],
                Privileges          = roleDoc.Contains("privileges") && roleDoc["privileges"].IsBsonArray 
                    ? ParsePrivileges(roleDoc["privileges"].AsBsonArray) 
                    : [],
                InheritedPrivileges = roleDoc.Contains("inheritedPrivileges") && roleDoc["inheritedPrivileges"].IsBsonArray 
                    ? ParsePrivileges(roleDoc["inheritedPrivileges"].AsBsonArray) 
                    : []
            };
            model.Roles.Add(role);
        }

        return model;
    }

    private static List<PrivilegeModel> ParsePrivileges(BsonArray privilegesArray)
    {
        var privileges = new List<PrivilegeModel>();

        foreach (var privilegeElement in privilegesArray)
        {
            var privilegeDoc = privilegeElement.AsBsonDocument;
            var resourceDoc = privilegeDoc["resource"].AsBsonDocument;

            var privilege = new PrivilegeModel
            {
                Resource = new()
                {
                    AnyResource= resourceDoc.Contains("anyResource")   && resourceDoc["anyResource"].AsBoolean,
                    Db         = resourceDoc.Contains("db")             ? resourceDoc["db"].AsString : string.Empty,
                    Collection = resourceDoc.Contains("collection")     ? resourceDoc["collection"].AsString : string.Empty,
                    Cluster    = resourceDoc.Contains("cluster")       && resourceDoc["cluster"].AsBoolean,
                    Buckets    = resourceDoc.Contains("system_buckets") ? resourceDoc["system_buckets"].AsString : null,
                },
                Actions = privilegeDoc.Contains("actions") && privilegeDoc["actions"].IsBsonArray
                    ? privilegeDoc["actions"].AsBsonArray.Select(a => a.AsString).ToList()
                    : []
            };

            privileges.Add(privilege);
        }

        return privileges;
    }
}

public class RoleInfoModel
{
    public override string ToString() => $"{RoleName} ({Db})";

    public string               RoleName            { get; set; } = string.Empty;
    public string               Db                  { get; set; } = string.Empty;
    public bool                 IsBuiltin           { get; set; }
    public List<RoleInDbModel>  Roles               { get; set; } = [];
    public List<PrivilegeModel> Privileges          { get; set; } = [];
    [JsonIgnore]
    public List<string>         InheritedRoles      { get; set; } = [];
    [JsonIgnore]
    public List<PrivilegeModel> InheritedPrivileges { get; set; } = [];

    public void CopyFrom(RoleInfoModel other)
    {
        RoleName       = other.RoleName;
        Db             = other.Db;
        IsBuiltin      = other.IsBuiltin;
        Roles          = new(other.Roles);
        InheritedRoles = new(other.InheritedRoles);
        Privileges = other.Privileges.Select(p => new PrivilegeModel
        {
            Resource = new()
            {
                AnyResource = p.Resource.AnyResource,
                Db          = p.Resource.Db,
                Collection  = p.Resource.Collection,
                Cluster     = p.Resource.Cluster,
                Buckets     = p.Resource.Buckets
            },
            Actions = [..p.Actions]
        }).ToList();
        InheritedPrivileges = other.InheritedPrivileges.Select(p => new PrivilegeModel
        {
            Resource = new()
            {
                AnyResource = p.Resource.AnyResource,
                Db          = p.Resource.Db,
                Collection  = p.Resource.Collection,
                Cluster     = p.Resource.Cluster,
                Buckets     = p.Resource.Buckets
            },
            Actions = [..p.Actions]
        }).ToList();
    }

    public RoleInfoModel Clone()
    {
        var clone = new RoleInfoModel();
        clone.CopyFrom(this);
        return clone;
    }

    public BsonDocument CreateUpdateRoleCommand(bool add)
    {
        var command = new BsonDocument
        {
            {  add ? "createRole" : "updateRole", RoleName },
            { "privileges", new BsonArray(
                Privileges.Select(p => new BsonDocument
                {
                    { "resource", ToBsonDocument(p.Resource) },
                    { "actions", new BsonArray(p.Actions) }
                })
            ) },
            { "roles", new BsonArray( 
                Roles.Select(x => 
                    string.IsNullOrWhiteSpace(x.Db)
                    ? BsonValue.Create(x.Role)
                    : new BsonDocument
                        {
                            { "role", x.Role }, 
                            { "db", x.Db } 
                        } 
                    ) 
                ) 
            },
            // { "authenticationRestrictions", new BsonArray(
            //     role.AuthenticationRestrictions.Select(ar => new BsonDocument
            //     {
            //         { "clientSource", new BsonArray(ar.ClientSource) },
            //         { "serverAddress", new BsonArray(ar.ServerAddress) }
            //     })
            // ) },
            { "writeConcern", new BsonDocument { { "w", "majority" } } }
        };
        return command;
    }

    private static BsonDocument ToBsonDocument(ResourceModel resource)  
    {  
        var resourceDoc = new BsonDocument();  

        if (resource.Cluster)  
        {  
            resourceDoc.Add("cluster", true);  
        }  
        else if (resource.AnyResource)
        {
            resourceDoc.Add("anyResource", true);
        }
        else if ( string.IsNullOrWhiteSpace(resource.Buckets) )
        {  
            resourceDoc.Add("db",         resource.Db);  
            resourceDoc.Add("collection", resource.Collection);  
        }
        else 
        {
            resourceDoc.Add("system_buckets", resource.Buckets);
        }

        return resourceDoc;  
    }

}

public class RoleInDbModel
{
    public string Db   { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public override string ToString() => $"{Role}, {Db}";

    public override bool Equals(object? obj)
    {
        if (obj is not RoleInDbModel other)
            return false;

        return Db == other.Db && Role == other.Role;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Db, Role);
    }
}

public class PrivilegeModel
{
    public override string ToString() => $"{Resource} => {string.Join(", ", Actions)}";

    public ResourceModel Resource { get; set; } = new();
    public List<string>  Actions  { get; set; } = [];
}

public class ResourceModel
{
    public override string ToString() => $"Db={Db}, Collection={Collection}, Any={AnyResource}, Cluster={Cluster} {Buckets}";

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool AnyResource { get; set; }
    public string  Db          { get; set; } = string.Empty;
    public string  Collection  { get; set; } = string.Empty;
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool    Cluster     { get; set;}
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Buckets     { get; set; }
}