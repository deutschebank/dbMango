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
﻿using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Rms.Risk.Mango.Interfaces;
using Rms.Service.Bootstrap.Security;

namespace Rms.Risk.Mango.Services.Context;

// ReSharper disable InconsistentNaming
public class DatabaseConfigurationService : IDatabaseConfigurationService
// ReSharper restore InconsistentNaming
{
    public static TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    private readonly IDatabaseConfigurationStorage? _storage;
    private readonly IOptions<DatabasesConfig>      _config;
    private readonly IPasswordManager               _passwordManager;

    public const string ContextTypeNameDbMangoDatabase    = "dbMangoDatabase";

    public DatabaseConfigurationService(
        IOptions<DatabasesConfig>      config,
        IPasswordManager               passwordManager,
        IDatabaseConfigurationStorage? storage = null
        )
    {
        _storage         = storage;
        _config          = config;
        _passwordManager = passwordManager;

        Databases = _config.Value.Databases.ToDictionary(x => x.Key, x => x.Value.Clone());

        var cts    = new CancellationTokenSource(DefaultTimeout);
        _ = Task.Run(Reload, cts.Token);
    }

    private ConcurrentDictionary<string,DbMangoDatabaseConfigContext> RawDatabases { get; set; } = [];
    public  Dictionary<string, DatabasesConfig.DatabaseConfig>        Databases    { get; set; }

    public async Task Reload()
    {
        if ( _storage == null)
            return;

        var cts    = new CancellationTokenSource(DefaultTimeout);
        var raw = await _storage.List("dbMango", cts.Token);
        RawDatabases = new(raw.ToDictionary(x => x.Name, x => x));

        var newDb = _config.Value.Databases
            .Where(x => !x.Key.StartsWith("<")) // skip <initial-database-name-placeholder>
            .ToDictionary(x => x.Key, x => x.Value.Clone());

        foreach (var databaseConfig in RawDatabases.Select( x => (x.Key, Config : ConvertTo(x.Value))))
        {
            newDb[databaseConfig.Key] =  databaseConfig.Config;
        }

        Databases = newDb;
    }

    public async Task Update(string name, DatabasesConfig.DatabaseConfig c, string email)
    {
        await Reload();

        if (RawDatabases.ContainsKey(name))
            await UpdateInternal(name, c, email);
        else
            await CreateInternal(name, c, email);
    }

    private async Task CreateInternal(string name, DatabasesConfig.DatabaseConfig c, string email)
    {
        var ctx = ConvertFrom(name, c, true);

        if ( _storage != null )
        {
            var cts    = new CancellationTokenSource(DefaultTimeout);
            await _storage.Create(ctx, email, cts.Token);
        }

        RawDatabases[name] = ctx;
    }

    private async Task UpdateInternal(string name, DatabasesConfig.DatabaseConfig c, string email)
    {

        var ctx = ConvertFrom(name, c, false);

        if ( _storage != null )
        {
            var cts    = new CancellationTokenSource(DefaultTimeout);
            await _storage.Update(ctx, email, cts.Token);
        }

        RawDatabases[name] = ctx;
    }

    public async Task Delete(string name, string email)
    {
        await Reload();

        if (!RawDatabases.TryGetValue(name, out var raw))
            throw new ApplicationException($"Configuration for {name} is not loaded");

        if ( _storage != null )
        {
            var cts    = new CancellationTokenSource(DefaultTimeout);
            await _storage.Delete(raw.ID, email, cts.Token);
        }

        RawDatabases.Remove(name, out _);
    }


    private DatabasesConfig.DatabaseConfig ConvertTo(DbMangoDatabaseConfigContext ctx)
    {
        var userPass  = DecryptPassword(ctx.DatabaseParams.UserAuthPassword);
        var adminPass = DecryptPassword(ctx.DatabaseParams.AdminAuthPassword);

        var c = new DatabasesConfig.DatabaseConfig
        {
            Contacts = ctx.DatabaseParams.Contacts,
            Comments = ctx.DatabaseParams.Comments,
            Config = new()
            {
                MongoDbUrl                = ctx.DatabaseParams.MongoDbUrl,
                MongoDbDatabase           = ctx.DatabaseParams.MongoDbDatabase,
                DirectConnection          = ctx.DatabaseParams.DirectConnection,
                UseTls                    = ctx.DatabaseParams.UseTls,
                AllowShardAccess          = ctx.DatabaseParams.AllowShardAccess,
                DisableDbMangoCollections = ctx.DatabaseParams.DisableDbMangoCollections,

                Auth = new()
                {
                    User         = ctx.DatabaseParams.UserAuthUser,
                    Password     = userPass,
                    AuthDatabase = ctx.DatabaseParams.UserAuthAuthDatabase,
                    Method       = ctx.DatabaseParams.UserAuthMethod
                },
                AdminAuth = new()
                {
                    User         = ctx.DatabaseParams.AdminAuthUser,
                    Password     = adminPass,
                    AuthDatabase = ctx.DatabaseParams.AdminAuthAuthDatabase,
                    Method       = ctx.DatabaseParams.AdminAuthMethod
                }
            },
            Groups = ctx.LdapParams
        };

        if (string.IsNullOrWhiteSpace(c.Config.Auth.User) ||
            string.IsNullOrWhiteSpace(c.Config.Auth.Password))
        {
            c.Config.Auth = null;
        }

        if (string.IsNullOrWhiteSpace(c.Config.AdminAuth.User) ||
            string.IsNullOrWhiteSpace(c.Config.AdminAuth.Password))
        {
            c.Config.AdminAuth = null;
        }

        return c;
    }

    private string DecryptPassword(string? p) =>
        string.IsNullOrWhiteSpace(p)
            ? ""
            : p.StartsWith("*")
                ? _passwordManager.DecryptPassword(p)
                : p;

    private string EncryptPassword(string? p) =>
        string.IsNullOrWhiteSpace(p)
            ? ""
            : p.StartsWith("*")
                ? p
                : _passwordManager.EncryptPassword(p);

    private DbMangoDatabaseConfigContext ConvertFrom( string name, DatabasesConfig.DatabaseConfig c, bool isNew )
    {
        DbMangoDatabaseConfigContext? raw = null;
        if (!isNew && !RawDatabases.TryGetValue(name, out raw))
            throw new ApplicationException($"Configuration for {name} is not loaded");

        var userPass  = EncryptPassword(c.Config.Auth?.Password);
        var adminPass = EncryptPassword(c.Config.AdminAuth?.Password);

        var ctx = new DbMangoDatabaseConfigContext
        {
            Type                = ContextTypeNameDbMangoDatabase ,
            Name                = isNew ? name : raw!.Name,
            ID                  = isNew ? 0 : raw!.ID,
            IsTemplate          = !isNew && raw!.IsTemplate,
            ProposedForDeletion = !isNew && raw!.ProposedForDeletion,

            DatabaseParams = new()
            {
                Contacts                  = c.Contacts,
                Comments                  = c.Comments,
                MongoDbUrl                = c.Config.MongoDbUrl,
                MongoDbDatabase           = c.Config.MongoDbDatabase,
                DirectConnection          = c.Config.DirectConnection,
                UseTls                    = c.Config.UseTls,
                AllowShardAccess          = c.Config.AllowShardAccess,
                DisableDbMangoCollections = c.Config.DisableDbMangoCollections,

                UserAuthUser          = c.Config.Auth?.User ?? "",
                UserAuthPassword      = userPass,
                UserAuthAuthDatabase  = c.Config.Auth?.AuthDatabase,
                UserAuthMethod        = c.Config.Auth?.Method,
                
                AdminAuthUser         = c.Config.AdminAuth?.User ?? "",
                AdminAuthPassword     = adminPass,
                AdminAuthAuthDatabase = c.Config.AdminAuth?.AuthDatabase,
                AdminAuthMethod       = c.Config.AdminAuth?.Method,
            },
            LdapParams = c.Groups
        };

        return ctx;
    }

}