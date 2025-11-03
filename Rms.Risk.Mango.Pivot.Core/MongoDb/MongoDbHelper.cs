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
using System.Diagnostics.CodeAnalysis;
using System.Net.Security;
using System.Reflection;
using System.Text.RegularExpressions;
using log4net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Rms.Risk.Mango.Pivot.Core.Models;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public static class MongoDbHelperConfig
{
    // ReSharper disable ConvertToConstant.Global
    // ReSharper disable InconsistentNaming
#pragma warning disable CA2211

    public static bool MongoDbHelper_ShouldDispose          = false;
    public static int  MongoDbHelper_ExpirationMinutes      = 30;
    public static int  MongoDbHelper_CleanupIntervalMinutes = 5;

#pragma warning restore CA2211
    // ReSharper restore InconsistentNaming
    // ReSharper restore ConvertToConstant.Global
}

[ExcludeFromCodeCoverage]
public static class MongoDbHelper
{
    private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

    private static          bool   _initialised;
    private static readonly Lock _globalSyncObject = new ();

    public static RemoteCertificateValidationCallback? RemoteCertificateCheck { get; set; }

    private static void Init()
    {
        if ( _initialised )
            return;

        lock (_globalSyncObject)
        {
            if ( _initialised )
                return;
            try
            {
                InitUnsafe();
            }
            finally
            {
                _initialised = true;
            }
        }
    }

    private static void InitUnsafe()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(MongoDatabaseInfo)))
            BsonClassMap.RegisterClassMap<MongoDatabaseInfo>(cm =>
            {
                cm.MapProperty(c => c.Name).SetElementName("name");
                cm.MapProperty(c => c.SizeOnDisk).SetElementName("sizeOnDisk");
                cm.MapProperty(c => c.Empty).SetElementName("empty");
            });
    }


    private static DateTime       _lastChecked;

    public static bool IsConnected(IMongoDatabase? database, bool force = false)
    {
        if (database == null)
            return false;

        try
        {
            lock (_globalSyncObject)
            {
                if (_lastChecked + TimeSpan.FromMinutes(1) < DateTime.Now && force == false)
                    return true;

                if (IsAlive(database, TimeSpan.FromSeconds(5), out _))
                {
                    _lastChecked = DateTime.Now;
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            _log.Warn("MongoDB connectivity lost. Reconnecting...", e);
        }

        return false;
    }

    private class MongoDbClientRecord(MongoClient client, IMongoDatabase database)
    {
        public IMongoDatabase Database    { get; } = database ?? throw new ArgumentNullException(nameof(database));
        // ReSharper disable once UnusedMember.Local
        public MongoClient    Client      { get; } = client ?? throw new ArgumentNullException(nameof(client));
        public DateTime       LastChecked { get; set; }  = DateTime.Now;
    }

    private static readonly Lazy<ExpiringConcurrentDictionary<string, MongoDbClientRecord>> _databases = new ( () => new(
        TimeSpan.FromMinutes(MongoDbHelperConfig.MongoDbHelper_ExpirationMinutes), 
        TimeSpan.FromMinutes(MongoDbHelperConfig.MongoDbHelper_CleanupIntervalMinutes), 
        MongoDbHelperConfig.MongoDbHelper_ShouldDispose
        ) );

    /// <summary>
    /// Connect to the database with retries.
    /// </summary>
    public static IMongoDatabase GetDatabase( 
        MongoDbConfigRecord config,
        MongoDbSettings     mongoSettings,
        string?             databaseInstance = null
    )  
    {
        var key = config.GetKey(databaseInstance);
        Exception? firstException = null;

        for ( var  i = 0 ; i < mongoSettings.MongoDbConnectionRetries; i++ )
        {
            var db = _databases.Value.GetOrAdd(
                key,
                _ =>
                {
                    var rec = GetDatabaseInternal(config, mongoSettings, databaseInstance);
                    return rec;
                }
            );

            if ( db.LastChecked + TimeSpan.FromMinutes(1) < DateTime.Now )
            {
                if ( !IsAlive(db.Database, TimeSpan.FromSeconds(mongoSettings.MongoDbPingTimeoutSec), out var exception))
                {
                    firstException ??= exception;
                    _databases.Value.TryRemove(key);
                    continue; // Try to reconnect
                }
                db.LastChecked = DateTime.Now;
            }

            return db.Database;
        }

        throw new ApplicationException($"Failed to connect to MongoDB after {mongoSettings.MongoDbConnectionRetries} retries. {key}", firstException);
    }

    private static MongoDbClientRecord GetDatabaseInternal( 
        MongoDbConfigRecord config,
        MongoDbSettings mongoSettings,
        string? databaseInstance = null
        ) => RetryHelper.DoWithRetries( () =>
        {
            Init();

            var database = databaseInstance ?? config.MongoDbDatabase;

            _log.Debug($"Connecting to {config.GetKey(database)}");

            var settings = GetMongoClientSettings(config, mongoSettings,database == "admin");
        
            var client = new MongoClient(settings);
            var db = client.GetDatabase(database);

            if (!IsAlive(db, TimeSpan.FromSeconds(mongoSettings.MongoDbPingTimeoutSec), out var exception))
                throw new ApplicationException($"{exception?.Message} MongoDB connectivity lost {config.GetKey(database)}", exception);

            return new MongoDbClientRecord(client, db);
        },
        mongoSettings.MongoDbConnectionRetries,
        TimeSpan.FromSeconds(mongoSettings.MongoDbRetryTimeoutSec),
        null,
        LogMethod
        );

    private static bool IsAlive(IMongoDatabase db, TimeSpan pingTimeout, out Exception? exception )
    {
        try
        {
            exception = null;
            var cts         = new CancellationTokenSource(pingTimeout);
            var isMongoLive = db.RunCommand((Command<BsonDocument>)"{ping:1}", cancellationToken: cts.Token);
        
            if (isMongoLive == null)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            exception = ex;
            return false;
        }
    }

    /// <summary>
    /// Log method for retry function, called for each iteration
    /// </summary>
    /// <param name="iteration"></param>
    /// <param name="maxRetries"></param>
    /// <param name="e"></param>
    private static void LogMethod(int iteration, int maxRetries, Exception e)
    {
        if (iteration < maxRetries - 1)
            _log.Warn($"MongoDB connection error, retrying  RetriesLeft={maxRetries - iteration - 1}\": {e.Message}", e);
        else
            _log.Error($"MongoDB connection error, retries exhausted: {e.Message}", e);
    }

    public static MongoClientSettings GetMongoClientSettings( 
        MongoDbConfigRecord config,
        MongoDbSettings mongoSettings,
        bool toAdmin
        )
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        var settings = MongoClientSettings.FromUrl( new( config.MongoDbUrl ) );

        var auth = toAdmin
            ? config.AdminAuth ?? config.Auth
            : config.Auth;

        if ( !string.IsNullOrWhiteSpace(auth?.User) && !string.IsNullOrWhiteSpace(auth.Password) )
        {
            var credential = new MongoCredential(
                auth.Method,
                new MongoInternalIdentity(auth.AuthDatabase ?? "admin", auth.User),
                new PasswordEvidence(auth.Password));

            settings.Credential = credential;
        }

        settings.SocketTimeout          = mongoSettings.MongoDbSocketTimeout;
        settings.ServerSelectionTimeout = mongoSettings.MongoDbServerSelectionTimeout;
        settings.ConnectTimeout         = mongoSettings.MongoDbConnectTimeout;
        settings.MinConnectionPoolSize  = mongoSettings.MongoDbMinConnectionPoolSize;
        settings.MaxConnectionPoolSize  = mongoSettings.MongoDbMaxConnectionPoolSize;
        settings.MaxConnectionIdleTime  = mongoSettings.MaxConnectionIdleTime;
        settings.MaxConnectionLifeTime  = mongoSettings.MaxConnectionLifeTime;
        settings.DirectConnection       = config.DirectConnection;
        settings.UseTls                 = config.UseTls;

        if (RemoteCertificateCheck != null)
            settings.SslSettings.ServerCertificateValidationCallback = RemoteCertificateCheck;

        // This doc seems to suggest we can turn writeretries on (as we have mongos, with a sharded cluster) 
        // https://docs.mongodb.com/manual/core/retryable-writes/  But when we enable it we get an exception 
        //"One or more errors occurred. (A bulk write operation resulted in one or more errors. Transaction numbers are only allowed on a replica set member or mongos"

        settings.RetryReads  = false;
        settings.RetryWrites = false;
        return settings;
    }

    private static int _counter;

    public static string GetTempCollectionName(string? prefix = null) =>
        string.IsNullOrWhiteSpace(prefix) 
            ? $"Temp-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-{Interlocked.Increment(ref _counter)}"
            : $"Temp-{prefix}-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-{Interlocked.Increment(ref _counter)}";


    public static HashSet<string> ExtractDuplicateKeys( Exception ex )
    {
        var rx = new Regex( """.*E11000 duplicate key error collection.*\{[^\:]*: "(?<Id>[^"]+)".*""",
                            RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.Singleline );


        var toProcess = new HashSet<string>(
            ex.Message
              .Split( "Category" )
              .Select( x =>
               {
                   var m = rx.Match( x );
                   return m.Success
                       ? m.Groups["Id"].Value
                       : null;
               } )
              .Where( x => x != null )
              .OfType<string>()
        );
        return toProcess;
    }

    public static bool IsDuplicateKeyError( Exception ex ) => ex.Message.IndexOf("duplicate key error", StringComparison.Ordinal) > 0;
}
