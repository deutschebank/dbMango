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
﻿using Microsoft.Extensions.Options;
using MongoDB.Bson;
using Rms.Risk.Mango.Interfaces;
using Rms.Risk.Mango.Pivot.Core;
using Rms.Risk.Mango.Pivot.Core.Models;
using Rms.Risk.Mango.Pivot.Core.MongoDb;
using Rms.Risk.Mango.Services.Audit;
using Rms.Risk.Mango.Services.Context;

namespace Rms.Risk.Mango.Services;

public class MongoDbServiceFactory : IMongoDbServiceFactory
{
    private const int SessionExpirationMinutes = 30;
    private const int SessionExpirationCheckMinutes = 5;

    private record AuditServiceKey(string  AuditDatabase, string DatabaseInstance);
    private record PivotServiceKey(string  Database,      string DatabaseInstance);
    private record AdminServiceKey (string Database,      string DatabaseInstance, IUserSession Session);

    private readonly IDatabaseConfigurationService _databases;
    private readonly IOptions<DbMangoSettings>     _settings;
    private          IAuditService?                _inOracle;

    private readonly ExpiringConcurrentDictionary<AuditServiceKey, IAuditService>                _auditServices;
    private readonly ExpiringConcurrentDictionary<PivotServiceKey, IPivotTableDataSource>        _pivotServices;
    private readonly ExpiringConcurrentDictionary<AdminServiceKey, IMongoDbDatabaseAdminService> _adminServices;
    private readonly IDbMangoPlugin?                                                             _dbMangoPlugin;
    private readonly ILogger<MongoDbServiceFactory>                                              _log;
    private readonly ILogger<AuditService>                                                       _auditLog;

    public MongoDbServiceFactory(
        IDatabaseConfigurationService  databases, 
        IOptions<DbMangoSettings>      settings, 
        ILogger<MongoDbServiceFactory> log, 
        ILogger<AuditService>          auditLog,
        IDbMangoPlugin? dbMangoPlugin = null)
    {
        _log           = log;
        _auditLog      = auditLog;
        _databases     = databases;
        _settings      = settings;
        _dbMangoPlugin = dbMangoPlugin;

        _auditServices = new (
            InternalCreateAudit,
            TimeSpan.FromMinutes(SessionExpirationMinutes),
            TimeSpan.FromMinutes(SessionExpirationCheckMinutes),
            false
        );
        _adminServices = new (
            InternalCreateAdmin,
            TimeSpan.FromMinutes(SessionExpirationMinutes),
            TimeSpan.FromMinutes(SessionExpirationCheckMinutes),
            false
        );
        _pivotServices = new (
            InternalCreatePivot,
            TimeSpan.FromMinutes(SessionExpirationMinutes),
            TimeSpan.FromMinutes(SessionExpirationCheckMinutes),
            false
        );
    }

    public static MongoDbConfigRecord GetConfig(MongoDbConfigRecord config, string databaseInstance)
    {
        if (string.IsNullOrWhiteSpace(config.MongoDbDatabase) && string.IsNullOrWhiteSpace(databaseInstance))
            throw new ApplicationException("MongoDB database name is not provided and it's not set in the configuration either.");

        if (!string.IsNullOrWhiteSpace(databaseInstance))
        {
            var copy = config.Clone();
            copy.MongoDbDatabase = databaseInstance;
            return copy;
        }

        return config;
    }

    public IPivotTableDataSource CreatePivot(string database, string databaseInstance)
        => _pivotServices.GetOrAdd(new(database,databaseInstance));

    public IMongoDbDatabaseAdminService CreateAdmin(string database, IUserSession session, string databaseInstance)
        => _adminServices.GetOrAdd(new(database, databaseInstance, session));

    public IMongoDbService<BsonDocument> Create(string database, string collection, string databaseInstance)
        => new BsonMongoDbService(GetConfig(_databases.Databases[database].Config, databaseInstance), _settings.Value.Settings, collection, databaseInstance);

    public IMongoDbDatabaseAdminService CreateAdmin(MongoDbConfigRecord config, string auditDatabase, IUserSession session, string databaseInstance)
        => new AuditedMongoDbDatabaseAdminService(
            config,
            _settings.Value.Settings,
            session,
            CreateAudit(auditDatabase, string.IsNullOrWhiteSpace(config.MongoDbDatabase) ? databaseInstance : config.MongoDbDatabase), databaseInstance);

    public IAuditService CreateAudit(string auditDatabase, string databaseInstance)
        => _auditServices.GetOrAdd(new(auditDatabase,databaseInstance));

#region Internal Methods

    private IPivotTableDataSource InternalCreatePivot(PivotServiceKey key) =>
        new MongoDbOnlyProxyDataSource(new MongoDbDataSource(
                                           GetConfig(_databases.Databases[key.Database].Config, key.DatabaseInstance),
                                           _settings.Value.Settings,
                                           key.DatabaseInstance 
                                       ));

    private IMongoDbDatabaseAdminService InternalCreateAdmin(AdminServiceKey key)
        => new AuditedMongoDbDatabaseAdminService(
            GetConfig(_databases.Databases[key.Database].Config, key.DatabaseInstance),
            _settings.Value.Settings,
            key.Session,
            CreateAudit(key.Database, key.DatabaseInstance), key.DatabaseInstance);

    private IAuditService InternalCreateAudit(AuditServiceKey key)
    {

        var inMongo = new AuditService(
            GetConfig(_databases.Databases[key.AuditDatabase].Config, key.DatabaseInstance),
            _settings.Value.Settings,
            _settings.Value.AuditExpireDays,
            _auditLog
        );

        _inOracle ??= _settings.Value.AuditLogsInOracle && _dbMangoPlugin != null
            ? _dbMangoPlugin.CreateSecureAuditService(_settings.Value.OracleConnectionSettings)
            : null;

        if ( _settings.Value.AuditLogsInOracle && _dbMangoPlugin == null )
        {
            _log.LogWarning("AuditLogsInOracle is enabled but no DbMangoPlugin is loaded, so Oracle audit logging is disabled.");
        }
        _log.LogDebug($"Creating AuditService for database '{key.AuditDatabase}': InOracle={_inOracle != null} PluginLoaded={_dbMangoPlugin != null}");

        return _inOracle == null
            ? inMongo
            : new ChainedAuditService([_inOracle, inMongo]);
    }

#endregion
}