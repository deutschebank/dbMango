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
using MongoDB.Driver;

namespace Rms.Risk.Mango.Pivot.Core.MongoDb;

public class MongoDbAuth
{
    public string  User         { get; set; } = MongoDbConfig.MongoDbUser    ;
    public string  Password     { get; set; } = MongoDbConfig.MongoDbPassword;
    public string? AuthDatabase { get; set; } = MongoDbConfig.MongoDbAuthDatabase;
    public string? Method       { get; set; } = MongoDbConfig.MongoDbAuthMethod;

    public MongoDbAuth Clone()
        => new()
        {
            User         = User        ,
            Password     = Password    ,
            AuthDatabase = AuthDatabase,
            Method       = Method
        };
}

public class MongoDbSettings
{
    public TimeSpan MongoDbSocketTimeout          { get; set; } = MongoDbConfig.MongoDbSocketTimeout         ;
    public TimeSpan MongoDbServerSelectionTimeout { get; set; } = MongoDbConfig.MongoDbServerSelectionTimeout;
    public TimeSpan MongoDbConnectTimeout         { get; set; } = MongoDbConfig.MongoDbConnectTimeout        ;
    public int      MongoDbMinConnectionPoolSize  { get; set; } = MongoDbConfig.MongoDbMinConnectionPoolSize ;
    public int      MongoDbMaxConnectionPoolSize  { get; set; } = MongoDbConfig.MongoDbMaxConnectionPoolSize ;
    public TimeSpan MaxConnectionIdleTime         { get; set; } = MongoDbConfig.MongoDbMaxConnectionIdleTime        ;
    public TimeSpan MaxConnectionLifeTime         { get; set; } = MongoDbConfig.MongoDbMaxConnectionLifeTime        ;
    public TimeSpan MongoDbQueryTimeout           { get; set; } = MongoDbConfig.MongoDbQueryTimeout          ;
    public int      MongoDbPingTimeoutSec         { get; set; } = MongoDbConfig.MongoDbPingTimeoutSec        ;
    public int      MongoDbQueryBatchSize         { get; set; } = MongoDbConfig.MongoDbQueryBatchSize        ;
    public int      MongoDbConnectionRetries      { get; set; } = MongoDbConfig.MongoDbConnectionRetries     ;
    public int      MongoDbRetryTimeoutSec        { get; set; } = MongoDbConfig.MongoDbRetryTimeoutSec       ;
    public TimeSpan CollectionsCachingPeriod      { get; set; } = TimeSpan.FromHours(2);
}

public class MongoDbConfigRecord
{
    public string       MongoDbUrl                { get; set; } = MongoDbConfig.MongoDbUrl                   ;
    public string       MongoDbDatabase           { get; set; } = MongoDbConfig.MongoDbDatabase              ;
    public MongoDbAuth? Auth                      { get; set; }
    public MongoDbAuth? AdminAuth                 { get; set; }
    public bool         DirectConnection          { get; set; } = MongoDbConfig.MongoDbDirectConnection             ;
    public bool         UseTls                    { get; set; } = MongoDbConfig.MongoDbUseTls                       ;
    public bool         SendClientCertificates    { get; set; } = MongoDbConfig.MongoDbSendClientCertificates;
    public bool         AllowShardAccess          { get; set; } = MongoDbConfig.MongoDbAllowShardAccess;
    public bool         DisableDbMangoCollections { get; set; }

    public override string ToString() => GetKey();

    public string GetKey(string? databaseInstance = null)
        => $"Url=\"{MongoDbUrl}\" Database=\"{databaseInstance ?? MongoDbDatabase}\" Direct={DirectConnection} Tls={UseTls} User=\"{Auth?.User ?? "null"}\" AdminUser=\"{AdminAuth?.User ?? Auth?.User ?? "null"}\"";

    public MongoDbConfigRecord Clone()
        => new()
        {
            MongoDbUrl                = MongoDbUrl         ,
            MongoDbDatabase           = MongoDbDatabase    ,
            Auth                      = Auth?.Clone()      ,
            AdminAuth                 = AdminAuth?.Clone() ,
            DirectConnection          = DirectConnection   ,
            UseTls                    = UseTls             ,
            AllowShardAccess          = AllowShardAccess   ,
            DisableDbMangoCollections = DisableDbMangoCollections
        };

    public void Check()
    {
        if (MongoDbUrl.StartsWith("<<"))
            throw new ApplicationException($"Invalid MongoDB URL: {MongoDbUrl}");
    }
}

/// <summary>
/// Do not use these values directly. Always use see  <see cref="MongoDbConfig.GetConfigRecord"/>.
/// </summary>
public static class MongoDbConfig
{
    // MongoDbConfigRecord
    public static string MongoDbUrl           = "<<NOT SET>>";
    public static string MongoDbDatabase      = "System";
    public static string MongoDbUser          = "";
    public static string MongoDbPassword      = "";
    public static string MongoDbAuthMethod    = "";
    public static string MongoDbAuthDatabase  = "admin";
    public static string MongoDbAdminUser     = "";
    public static string MongoDbAdminPassword = "";
    public static string MongoDbAdminDatabase = "admin";
    public static bool   MongoDbDirectConnection;
    public static bool   MongoDbAllowShardAccess;
    public static bool   MongoDbUseTls;
    public static bool   MongoDbSendClientCertificates;

    // MongoDbSettings
    public static TimeSpan MongoDbSocketTimeout          = MongoDefaults.SocketTimeout;
    public static TimeSpan MongoDbServerSelectionTimeout = MongoDefaults.ServerSelectionTimeout;
    public static TimeSpan MongoDbConnectTimeout         = MongoDefaults.ConnectTimeout;
    public static int      MongoDbMinConnectionPoolSize  = MongoDefaults.MinConnectionPoolSize;
    public static int      MongoDbMaxConnectionPoolSize  = MongoDefaults.MaxConnectionPoolSize;
    public static TimeSpan MongoDbMaxConnectionIdleTime  = MongoDefaults.MaxConnectionIdleTime; // TimeSpan.FromHours(8); // mongo default - 10 mins
    public static TimeSpan MongoDbMaxConnectionLifeTime  = MongoDefaults.MaxConnectionLifeTime; // TimeSpan.FromHours(8); // mongo default - 30 mins
    public static TimeSpan MongoDbQueryTimeout           = TimeSpan.FromMinutes(60);
    public static int      MongoDbPingTimeoutSec         = 60;
    public static int      MongoDbQueryBatchSize         = 5_000;
    public static int      MongoDbConnectionRetries      = 5;
    public static int      MongoDbRetryTimeoutSec        = 5;

    public static MongoDbConfigRecord GetConfigRecord()
    {
        var res = new MongoDbConfigRecord
        {
            MongoDbUrl             = MongoDbUrl,
            MongoDbDatabase        = MongoDbDatabase,
            DirectConnection       = MongoDbDirectConnection, 
            UseTls                 = MongoDbUseTls,
            SendClientCertificates = MongoDbSendClientCertificates,
            AllowShardAccess       = MongoDbAllowShardAccess
        };

        if ( !string.IsNullOrEmpty(MongoDbUser) || !string.IsNullOrEmpty(MongoDbPassword) )
        {
            res.Auth = new ()
            {
                User         = MongoDbUser,
                Password     = MongoDbPassword,
                AuthDatabase = MongoDbAuthDatabase,
                Method       = MongoDbAuthMethod
            };
        }

        if ( !string.IsNullOrEmpty(MongoDbAdminUser) || !string.IsNullOrEmpty(MongoDbAdminPassword) )
        {
            res.AdminAuth = new ()
            {
                User         = MongoDbAdminUser,
                Password     = MongoDbAdminPassword,
                AuthDatabase = MongoDbAdminDatabase,
                Method       = MongoDbAuthMethod
            };
        }

        res.Check();
        return res;
    }

    public static MongoDbSettings GetSettings()
    {
        var res =  new MongoDbSettings
        {
            MongoDbSocketTimeout          = MongoDbSocketTimeout,
            MongoDbServerSelectionTimeout = MongoDbServerSelectionTimeout,
            MongoDbConnectTimeout         = MongoDbConnectTimeout,
            MongoDbMinConnectionPoolSize  = MongoDbMinConnectionPoolSize,
            MongoDbMaxConnectionPoolSize  = MongoDbMaxConnectionPoolSize,
            MaxConnectionIdleTime         = MongoDbMaxConnectionIdleTime,
            MaxConnectionLifeTime         = MongoDbMaxConnectionLifeTime,
            MongoDbQueryTimeout           = MongoDbQueryTimeout,
            MongoDbPingTimeoutSec         = MongoDbPingTimeoutSec,
            MongoDbQueryBatchSize         = MongoDbQueryBatchSize,
            MongoDbConnectionRetries      = MongoDbConnectionRetries,
            MongoDbRetryTimeoutSec        = MongoDbRetryTimeoutSec,
        };
        return res;
    }

    public static void CopyFrom(MongoDbConfigRecord rec, MongoDbSettings settings)
    {
        rec.Check();

        MongoDbUrl                    = rec.MongoDbUrl;
        MongoDbDatabase               = rec.MongoDbDatabase;
        MongoDbUser                   = rec.Auth?.User ?? "";
        MongoDbPassword               = rec.Auth?.Password ?? "";
        MongoDbAuthDatabase           = rec.Auth?.AuthDatabase ?? "admin";
        MongoDbAuthMethod             = rec.Auth?.Method ?? "";
        MongoDbAdminUser              = rec.AdminAuth?.User ?? "";
        MongoDbAdminPassword          = rec.AdminAuth?.Password ?? "";
        MongoDbAdminDatabase          = rec.AdminAuth?.AuthDatabase ?? "admin";
        MongoDbDirectConnection       = rec.DirectConnection;
        MongoDbUseTls                 = rec.UseTls;
        MongoDbSendClientCertificates = rec.SendClientCertificates;
        MongoDbAllowShardAccess       = rec.AllowShardAccess;

        MongoDbSocketTimeout          = settings.MongoDbSocketTimeout;
        MongoDbServerSelectionTimeout = settings.MongoDbServerSelectionTimeout;
        MongoDbConnectTimeout         = settings.MongoDbConnectTimeout;
        MongoDbMinConnectionPoolSize  = settings.MongoDbMinConnectionPoolSize;
        MongoDbMaxConnectionPoolSize  = settings.MongoDbMaxConnectionPoolSize;
        MongoDbMaxConnectionIdleTime  = settings.MaxConnectionIdleTime;
        MongoDbMaxConnectionLifeTime  = settings.MaxConnectionLifeTime;
        MongoDbQueryTimeout           = settings.MongoDbQueryTimeout;
        MongoDbPingTimeoutSec         = settings.MongoDbPingTimeoutSec;
        MongoDbQueryBatchSize         = settings.MongoDbQueryBatchSize;
        MongoDbConnectionRetries      = settings.MongoDbConnectionRetries;
        MongoDbRetryTimeoutSec        = settings.MongoDbRetryTimeoutSec;
    }
}