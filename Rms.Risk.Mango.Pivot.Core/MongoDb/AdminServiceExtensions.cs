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
using Rms.Risk.Mango.Pivot.Core.Models;
using Rms.Risk.Mango.Services;
using Rms.Risk.Mango.Services.Models;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public class ListDatabasesResultItem
{
    public string Name    { get; set; } = string.Empty;
    public long   Size    { get; set; }
    public bool   IsEmpty { get; set; }
}

public class CollectionStats
{
    public string        Name           { get; init; } = "";
    public long          Size           { get; init; }
    public long          Count          { get; init; }
    public long          StorageSize    { get; init; }
    public long          TotalIndexSize { get; init; }
    public long          TotalSize      { get; init; }
    public bool          Sharded        { get; init; }
    public BsonDocument? Details        { get; set; } = new();
}

public static class AdminServiceExtensions
{
    public static async Task<List<ListDatabasesResultItem>> ListDatabases(
        this IMongoDbDatabaseAdminService service,
        CancellationToken token = default)
    {
        var dbs = await service.RunCommand(new ("listDatabases", 1), token);

        return dbs["databases"].AsBsonArray
            .Select(x => new ListDatabasesResultItem
            {
                Name    = x["name"].ToString() ?? "???",
                Size    = x["sizeOnDisk"].ToInt64(),
                IsEmpty = x["empty"].ToBoolean()
            })
            .ToList();
    }

    public static async Task<string> GetVersion(
        this IMongoDbDatabaseAdminService service,
        CancellationToken                 token = default)
    {
        var res = await service.RunCommand(new ("buildInfo", 1), token);

        return res["version"].ToString() ?? "0.0.0";
    }

    //public static async Task<bool> IsSharded( this IMongoDbDatabaseAdminService db, string database, string collectionName )
    //{
    //    try
    //    {
    //        var command = BsonDocument.Parse(
    //                $"{{ getShardVersion: \"{database}.{collectionName}\" }}" 
    //                );
    //        var doc = await db.RunCommand( command );
    //        return (int)(doc["ok"].ToDouble()) == 1;
    //    }
    //    catch ( Exception )
    //    {
    //        //if ( ex.Message.Contains( "is not sharded" ) || ex.Message.Contains( "does not have a routing table" ) )
    //        return false;
    //    }
    //}

    public static async Task<bool> IsSharded( this IMongoDbDatabaseAdminService db, string database, string collectionName )
    {
        var stats = await db.CollStats($"{database}.{collectionName}" );
        return stats.Sharded;
    }

    public static async Task<bool> IsSharded( this IMongoDbDatabaseAdminService db, string collectionName )
    {
        var stats = await db.CollStats(collectionName);
        return stats.Sharded;
    }

    public static async Task<CollectionStats> CollStats(
        this IMongoDbDatabaseAdminService service,
        string                            collectionName,
        CancellationToken                 token = default)
    {
        var command = new BsonDocument
        {
            { "collStats", collectionName },
            { "scale", 1 },
        };
        var res = await service.RunCommand(command, token);

        var stats = new CollectionStats
        {
            Name           = res["ns"].ToString() ?? "",
            Size           = res["size"].ToInt64(),
            Count          = res["count"].ToInt64(),
            StorageSize    = res["storageSize"].ToInt64(),
            TotalIndexSize = res["totalIndexSize"].ToInt64(),
            TotalSize      = res["totalSize"].ToInt64(),
            Sharded        = res.Contains("sharded") && res["sharded"].ToBoolean(),
            Details        = res
        };
        return stats;
    }

    public static async Task<CollStatsModel> CollStatsDetailed(
        this IMongoDbDatabaseAdminService service,
        string                            collectionName,
        CancellationToken                 token = default)
    {
        var command = new BsonDocument
        {
            { "collStats", collectionName },
            { "scale", 1 },
        };
        var res = await service.RunCommand(command, token);
        var json = res.ToJson();
        return CollStatsModel.FromJson(json);
    }

    public static async Task<DatabaseStatsModel> DbStats(
        this IMongoDbDatabaseAdminService service,
        CancellationToken                 token = default)
    {
        var command = new BsonDocument
        {
            { "dbStats", 1 },
            { "freeStorage", 1 },
        };
        var res = await service.RunCommand(command, token);
        return DatabaseStatsModel.FromBson(res);
    }

    public static async Task DropRole(
        this IMongoDbDatabaseAdminService service,
        string                            roleName,
        CancellationToken                 token    = default)
    {
        var command = new BsonDocument
        {
            { "dropRole", roleName },
            { "writeConcern",new BsonDocument
                { 
                    { "w", "majority" }
                }
            }
        };
        
        await service.RunCommand(command, token);
    }

    public static async Task CreateRole(
        this IMongoDbDatabaseAdminService service,
        RoleInfoModel              role,
        CancellationToken                 token    = default)
    {
        var command = role.CreateUpdateRoleCommand(true);
        await service.RunCommand(command, token);
    }

    public static async Task UpdateRole(
        this IMongoDbDatabaseAdminService service,
        RoleInfoModel                     role,
        CancellationToken                 token    = default)
    {
        var command = role.CreateUpdateRoleCommand(false);
        await service.RunCommand(command, token);
    }


    public static async Task<RolesInfoModel> GetRolesInfo(
        this IMongoDbDatabaseAdminService service,
        string? roleName = null,
        string? dbName   = null,
        bool showBuiltInRoles = true,
        CancellationToken                 token = default)
    {
        BsonDocument command;
        if ( string.IsNullOrWhiteSpace(roleName) ) 
        {
            command = new ()
            {
                { "rolesInfo", 1            },
                { "showPrivileges", true    },
                { "showBuiltinRoles", showBuiltInRoles  }
            };
        }
        else if (dbName == null)
        {
            command = new()
            {
                { "rolesInfo", roleName },
                { "showPrivileges", true },
                { "showBuiltinRoles", showBuiltInRoles }
            };
        }
        else
        {
            command = new()
            {
                { "rolesInfo", new BsonDocument
                    { 
                        { "role", roleName }, 
                        { "db", dbName } 
                    } 
                },
                { "showPrivileges", true },
                { "showBuiltinRoles", showBuiltInRoles }
            };
        }

        var res = await service.RunCommand(command, token);
        return RolesInfoModel.FromBson(res);
    }

    public static async Task<IndexesInfoModel> GetIndexes(
        this IMongoDbDatabaseAdminService service,
        string collectionName,
        CancellationToken                 token    = default)
    {
        var command = new BsonDocument()
        {
            { "listIndexes", collectionName  }
        };

        var res = await service.RunCommand(command, token);
        return IndexesInfoModel.FromBson(res);
    }

    public static async Task<UsersModel> GetUsersInfo(
        this IMongoDbDatabaseAdminService service,
        CancellationToken                 token    = default)
    {
        var command = new BsonDocument()
            {
                { "usersInfo", 1            }
            };

        var res = await service.RunCommand(command, token);
        return UsersModel.FromBson(res);
    }

    public static async Task DropUser(
        this IMongoDbDatabaseAdminService service,
        string userName,
        CancellationToken                 token    = default)
    {
        var command = new BsonDocument()
        {
            { "dropUser", userName          },
            { "writeConcern",new BsonDocument
                { 
                    { "w", "majority" }
                }
            }
        };

        _ = await service.RunCommand(command, token);
    }

    public static async Task<string[]> GetLogCategories(
        this IMongoDbDatabaseAdminService service,
        CancellationToken                 token = default
        )
    {
        var command = new BsonDocument
        {
            { "getLog", "*" }
        };
        var doc = await service.RunCommand(command, token);

        var logs = doc.Contains("names") ? doc["names"].AsBsonArray : new();

        return logs.Where(log => log.IsString).Select(log => log.AsString).ToArray();
    }

    public static async Task<List<LogRecordModel>> GetLogs(
        this IMongoDbDatabaseAdminService service,
        string                            logCategory = "global",
        CancellationToken                 token = default
        )
    {
        var command = new BsonDocument
        {
            { "getLog", logCategory }
        };
        var doc = await service.RunCommand(command, token);

        var logs = doc.Contains("log") ? doc["log"].AsBsonArray : new();
        var res = new List<LogRecordModel>();

        foreach (var log in logs)
        {
            if (log is BsonDocument d)
            {
                res.Add(LogRecordModel.FromBson(d));
            }
            else if (log.IsString)
            {
                try
                {
                    var bson = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<BsonDocument>(log.AsString);
                    res.Add(LogRecordModel.FromBson(bson));
                }
                catch
                {
                    // ignore
                }
            }
        }

        return res;
    }

}