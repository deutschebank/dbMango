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
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Services.Context;
using Rms.Risk.Mango.Services.Security;
using Rms.Service.Bootstrap.Security;

namespace Rms.Risk.Mango.Services;

internal class UserSession : IUserSession
{
    public UserService User => _user;

    public string? TaskNumber     { get; set; }
    public string? TaskCheckError { get; private set; }

    private          CheckerReply?                         _checkReply;
    private readonly UserService                           _user;
    private readonly IMongoDbServiceFactory                _mongoDbServiceFactory;
    private readonly IChangeNumberChecker                  _changeNumberChecker;
    private readonly IDatabaseConfigurationService         _databases;

    public UserSession(UserService                           user, 
                       IMongoDbServiceFactory                mongoDbServiceFactory, 
                       IChangeNumberChecker                  changeNumberChecker,
                       IDatabaseConfigurationService         databases)
    {
        _user                  = user;
        _mongoDbServiceFactory = mongoDbServiceFactory;
        _changeNumberChecker   = changeNumberChecker;
        _databases             = databases;
        
        Database         = _databases.Databases.Keys.FirstOrDefault("");
        DatabaseInstance = Database == "" ? "" : _databases.Databases[Database].Config.MongoDbDatabase;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IUserSession other)
            return false;

        return string.Equals(_user.GetEmail(), other.User.GetEmail(),  StringComparison.Ordinal) &&
               string.Equals(Database,         other.Database,         StringComparison.Ordinal) &&
               string.Equals(DatabaseInstance, other.DatabaseInstance, StringComparison.Ordinal) &&
               string.Equals(TaskNumber,       other.TaskNumber,       StringComparison.Ordinal);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(_user.GetEmail(), Database, DatabaseInstance, TaskNumber);
    // ReSharper restore NonReadonlyMemberInGetHashCode

    public async Task<bool> HasValidTask(bool checkExtraInfo)
    {
        TaskNumber     ??= "ITSK0000000000";
        TaskCheckError =   null;

        if (string.IsNullOrWhiteSpace(TaskNumber) || string.IsNullOrWhiteSpace(User.GetEmail()))
        {
            TaskCheckError = "Task number or user email are not set.";
            return false;
        }

        var now = DateTime.UtcNow;

        if (_checkReply == null)
        {
            _checkReply = await _changeNumberChecker.IsValid(TaskNumber, User.GetEmail(), checkExtraInfo ? _databases.Databases[Database].Comments : null, now);
            if (!_checkReply.IsValid)
            {
                TaskCheckError = _checkReply.ErrorMessage;
                TaskNumber = null;
                _checkReply = null;
                return false;
            }
            TaskCheckError = null;
        }
        
        if ( _checkReply == null )
        {
            TaskCheckError = "Task check reply is null.";
            return false;
        }

        var isOpen = DateTime.UtcNow > _checkReply.ValidFromUtc 
            && DateTime.UtcNow < _checkReply.ValidToUtc;

        if ( !isOpen)
        {
            TaskCheckError = $"Task {TaskNumber} is valid, but implementation window is not open (Start: {_checkReply.ValidFromUtc}, End: {_checkReply.ValidToUtc} UTC)";
            return false;
        }
        
        return true;
    }

    public async Task<bool> CanAccess(IAuthorizationService auth, string policyName, string databaseName)
    {
        var res = await auth.AuthorizeAsync(User.GetUser(), databaseName,
        [
            policyName switch
            {
                DatabaseAccessPolicyExtensions.ReadAccessPolicy => new ReadAccessRequirement(),
                DatabaseAccessPolicyExtensions.WriteAccessPolicy => new WriteAccessRequirement(),
                DatabaseAccessPolicyExtensions.AdminAccessPolicy => new AdminAccessRequirement(),
                _ => throw new ($"Policy name \"{policyName}\" is invalid. Expecting {DatabaseAccessPolicyExtensions.ReadAccessPolicy}, {DatabaseAccessPolicyExtensions.WriteAccessPolicy}, or {DatabaseAccessPolicyExtensions.AdminAccessPolicy}")
            }
        ]);

        return res.Succeeded;
    }

    private Dictionary<string, DatabasesConfig.DatabaseConfig> Databases => _databases.Databases;

    public string Database
    {
        get;
        set
        {
            if (field == value)
                return;

            Clear();
            field = value;

            var config = GetDatabaseConfig(Database);

            // this is to make sure that DatabaseChanged is only called once
            var newInstance = IsDatabaseInstanceSelectionAllowed
                    ? ""
                    : config.Config.MongoDbDatabase
                ;

            if (DatabaseInstance != newInstance)
                DatabaseInstance = newInstance;
            else
                DatabaseChanged?.Invoke();
        }
    }

    public bool IsDatabaseInstanceSelectionAllowed => IsInstanceSelectionAllowed(Database);

    public bool IsInstanceSelectionAllowed(string database)
    {
        if (string.IsNullOrWhiteSpace(database))
            return false;

        if (Databases.TryGetValue(database, out var config))
        {
            return string.IsNullOrWhiteSpace(config.Config.MongoDbDatabase);
        }

        return false;
    }

    private void Clear()
    {
    }

    public event Action? DatabaseChanged;

    public string Collection { get; set; } = "";

    public string DatabaseInstance
    {
        get
        {
            if ( !string.IsNullOrWhiteSpace(field) )
                return field;

            if (!string.IsNullOrWhiteSpace(Database)) 
                return "";

            var config = GetDatabaseConfig(Database);

            // this is to make sure that DatabaseChanged is only called once
            if ( !IsDatabaseInstanceSelectionAllowed )
                field = config.Config.MongoDbDatabase;

            return field;
        }
        set
        {
            if (field == value)
                return;
            Clear();
            field = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
            DatabaseChanged?.Invoke();
        }
    }

    public IMongoDbService<BsonDocument> MongoDb => GetCustomMongoDbService(Database, DatabaseInstance, Collection);

    public IMongoDbService<BsonDocument> GetCustomMongoDbService(string databaseName, string databaseInstance, string collection)
    {
        if (string.IsNullOrWhiteSpace(collection))
            throw new("Collection is not selected");

        _ = GetDatabaseConfig(databaseName);

        return _mongoDbServiceFactory.Create(databaseName, collection, databaseInstance);
    }

    public IMongoDbDatabaseAdminService MongoDbAdmin => GetCustomAdmin(Database, DatabaseInstance);

    public IMongoDbDatabaseAdminService GetCustomAdmin(string databaseName, string databaseInstance)
    {
        _ = GetDatabaseConfig(Database);
        return _mongoDbServiceFactory.CreateAdmin(databaseName, this, databaseInstance);
    }

    public IMongoDbDatabaseAdminService MongoDbAdminForAdminDatabase
    {
        get
        {
            _ = GetDatabaseConfig(Database);
            return _mongoDbServiceFactory.CreateAdmin(Database, this, "admin");
        }
    }

    public IPivotTableDataSource PivotDataSource
    {
        get
        {
            _ = GetDatabaseConfig(Database);
            return _mongoDbServiceFactory.CreatePivot(Database, DatabaseInstance);
        }
    }

    public IAuditService Audit
    {
        get
        {
            _ = GetDatabaseConfig(Database);
            return _mongoDbServiceFactory.CreateAudit(Database, DatabaseInstance);
        }
    }

    public IMongoDbDatabaseAdminService GetShardConnection(
        string  host, 
        int     port
        )
    {
        var config = GetDatabaseConfig(Database);

        var shardConfig = config.Config.Clone();
        shardConfig.DirectConnection = true;
        shardConfig.AllowShardAccess = false;
        shardConfig.MongoDbUrl       = $"mongodb://{host}:{port}";

        var db = _mongoDbServiceFactory.CreateAdmin(shardConfig, Database, this, DatabaseInstance);
        return db;
    }

    public MongoDbConfigRecord DatabaseConfig => GetDatabaseConfig(Database).Config;

    private DatabasesConfig.DatabaseConfig GetDatabaseConfig(string database)
    {
        if (!Databases.TryGetValue(database, out var cfg))
            throw new($"Database=\"{database}\" is not configured");
        return cfg;
    }

    public DatabasesConfig.DatabaseConfig.LdapGroups LdapGroups
    {
        get
        {
            if (!Databases.TryGetValue(Database, out var cfg))
                throw new($"Database=\"{Database}\" is not configured");
            return cfg.Groups;
        }
    }
}