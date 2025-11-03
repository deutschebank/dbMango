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
﻿namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class MongoDbConfigTests
{
    [Test]
    public void MongoDbAuth_Clone_ShouldReturnDeepCopy()
    {
        // Arrange
        var auth = new MongoDbAuth
        {
            User         = "testUser",
            Password     = "testPassword",
            AuthDatabase = "testDatabase",
            Method       = "SCRAM-SHA-256"
        };

        // Act
        var clonedAuth = auth.Clone();

        // Assert
        Assert.AreNotSame(auth, clonedAuth);
        Assert.AreEqual(auth.User,         clonedAuth.User);
        Assert.AreEqual(auth.Password,     clonedAuth.Password);
        Assert.AreEqual(auth.AuthDatabase, clonedAuth.AuthDatabase);
        Assert.AreEqual(auth.Method,       clonedAuth.Method);
    }

    [Test]
    public void MongoDbConfigRecord_Clone_ShouldReturnDeepCopy()
    {
        // Arrange
        var configRecord = new MongoDbConfigRecord
        {
            MongoDbUrl      = "mongodb://localhost:27017",
            MongoDbDatabase = "testDatabase",
            Auth = new MongoDbAuth
            {
                User         = "testUser",
                Password     = "testPassword",
                AuthDatabase = "testAuthDatabase",
                Method       = "SCRAM-SHA-256"
            },
            AdminAuth = new MongoDbAuth
            {
                User         = "adminUser",
                Password     = "adminPassword",
                AuthDatabase = "adminAuthDatabase",
                Method       = "SCRAM-SHA-256"
            },
            DirectConnection = true,
            UseTls           = true,
            AllowShardAccess = false
        };

        // Act
        var clonedConfig = configRecord.Clone();

        // Assert
        Assert.AreNotSame(configRecord, clonedConfig);
        Assert.AreEqual(configRecord.MongoDbUrl,      clonedConfig.MongoDbUrl);
        Assert.AreEqual(configRecord.MongoDbDatabase, clonedConfig.MongoDbDatabase);
        Assert.AreNotSame(configRecord.Auth,      clonedConfig.Auth);
        Assert.AreNotSame(configRecord.AdminAuth, clonedConfig.AdminAuth);
        Assert.AreEqual(configRecord.DirectConnection, clonedConfig.DirectConnection);
        Assert.AreEqual(configRecord.UseTls,           clonedConfig.UseTls);
        Assert.AreEqual(configRecord.AllowShardAccess, clonedConfig.AllowShardAccess);
    }

    [Test]
    public void MongoDbConfigRecord_Check_ShouldThrowExceptionForInvalidUrl()
    {
        // Arrange
        var configRecord = new MongoDbConfigRecord
        {
            MongoDbUrl = "<<NOT SET>>"
        };

        // Act & Assert
        var ex = Assert.Throws<ApplicationException>(() => configRecord.Check());
        Assert.AreEqual("Invalid MongoDB URL: <<NOT SET>>", ex?.Message);
    }

    [Test]
    public void MongoDbConfig_GetConfigRecord_ShouldReturnValidConfigRecord()
    {
        // Arrange
        MongoDbConfig.MongoDbUrl              = "mongodb://localhost:27017";
        MongoDbConfig.MongoDbDatabase         = "testDatabase";
        MongoDbConfig.MongoDbUser             = "testUser";
        MongoDbConfig.MongoDbPassword         = "testPassword";
        MongoDbConfig.MongoDbAuthDatabase     = "admin";
        MongoDbConfig.MongoDbAuthMethod       = "SCRAM-SHA-256";
        MongoDbConfig.MongoDbDirectConnection = true;
        MongoDbConfig.MongoDbUseTls           = true;
        MongoDbConfig.MongoDbAllowShardAccess = false;

        // Act
        var configRecord = MongoDbConfig.GetConfigRecord();

        // Assert
        Assert.AreEqual(MongoDbConfig.MongoDbUrl,              configRecord.MongoDbUrl);
        Assert.AreEqual(MongoDbConfig.MongoDbDatabase,         configRecord.MongoDbDatabase);
        Assert.AreEqual(MongoDbConfig.MongoDbDirectConnection, configRecord.DirectConnection);
        Assert.AreEqual(MongoDbConfig.MongoDbUseTls,           configRecord.UseTls);
        Assert.AreEqual(MongoDbConfig.MongoDbAllowShardAccess, configRecord.AllowShardAccess);
        Assert.IsNotNull(configRecord.Auth);
        Assert.AreEqual(MongoDbConfig.MongoDbUser,         configRecord.Auth!.User);
        Assert.AreEqual(MongoDbConfig.MongoDbPassword,     configRecord.Auth.Password);
        Assert.AreEqual(MongoDbConfig.MongoDbAuthDatabase, configRecord.Auth.AuthDatabase);
        Assert.AreEqual(MongoDbConfig.MongoDbAuthMethod,   configRecord.Auth.Method);
    }

    [Test]
    public void MongoDbConfig_GetSettings_ShouldReturnValidSettings()
    {
        // Arrange
        MongoDbConfig.MongoDbSocketTimeout          = TimeSpan.FromSeconds(30);
        MongoDbConfig.MongoDbServerSelectionTimeout = TimeSpan.FromSeconds(10);
        MongoDbConfig.MongoDbConnectTimeout         = TimeSpan.FromSeconds(5);
        MongoDbConfig.MongoDbMinConnectionPoolSize  = 1;
        MongoDbConfig.MongoDbMaxConnectionPoolSize  = 100;
        MongoDbConfig.MongoDbMaxConnectionIdleTime  = TimeSpan.FromMinutes(10);
        MongoDbConfig.MongoDbMaxConnectionLifeTime  = TimeSpan.FromHours(1);
        MongoDbConfig.MongoDbQueryTimeout           = TimeSpan.FromMinutes(2);
        MongoDbConfig.MongoDbPingTimeoutSec         = 60;
        MongoDbConfig.MongoDbQueryBatchSize         = 5000;
        MongoDbConfig.MongoDbConnectionRetries      = 3;
        MongoDbConfig.MongoDbRetryTimeoutSec        = 5;

        // Act
        var settings = MongoDbConfig.GetSettings();

        // Assert
        Assert.AreEqual(MongoDbConfig.MongoDbSocketTimeout,          settings.MongoDbSocketTimeout);
        Assert.AreEqual(MongoDbConfig.MongoDbServerSelectionTimeout, settings.MongoDbServerSelectionTimeout);
        Assert.AreEqual(MongoDbConfig.MongoDbConnectTimeout,         settings.MongoDbConnectTimeout);
        Assert.AreEqual(MongoDbConfig.MongoDbMinConnectionPoolSize,  settings.MongoDbMinConnectionPoolSize);
        Assert.AreEqual(MongoDbConfig.MongoDbMaxConnectionPoolSize,  settings.MongoDbMaxConnectionPoolSize);
        Assert.AreEqual(MongoDbConfig.MongoDbMaxConnectionIdleTime,  settings.MaxConnectionIdleTime);
        Assert.AreEqual(MongoDbConfig.MongoDbMaxConnectionLifeTime,  settings.MaxConnectionLifeTime);
        Assert.AreEqual(MongoDbConfig.MongoDbQueryTimeout,           settings.MongoDbQueryTimeout);
        Assert.AreEqual(MongoDbConfig.MongoDbPingTimeoutSec,         settings.MongoDbPingTimeoutSec);
        Assert.AreEqual(MongoDbConfig.MongoDbQueryBatchSize,         settings.MongoDbQueryBatchSize);
        Assert.AreEqual(MongoDbConfig.MongoDbConnectionRetries,      settings.MongoDbConnectionRetries);
        Assert.AreEqual(MongoDbConfig.MongoDbRetryTimeoutSec,        settings.MongoDbRetryTimeoutSec);
    }

    [Test]
    public void MongoDbConfig_CopyFrom_ShouldUpdateStaticConfig()
    {
        // Arrange
        var configRecord = new MongoDbConfigRecord
        {
            MongoDbUrl      = "mongodb://localhost:27017",
            MongoDbDatabase = "testDatabase",
            Auth = new MongoDbAuth
            {
                User         = "testUser",
                Password     = "testPassword",
                AuthDatabase = "admin",
                Method       = "SCRAM-SHA-256"
            },
            AdminAuth = new MongoDbAuth
            {
                User         = "adminUser",
                Password     = "adminPassword",
                AuthDatabase = "admin",
                Method       = "SCRAM-SHA-256"
            },
            DirectConnection = true,
            UseTls           = true,
            AllowShardAccess = false
        };

        var settings = new MongoDbSettings
        {
            MongoDbSocketTimeout          = TimeSpan.FromSeconds(30),
            MongoDbServerSelectionTimeout = TimeSpan.FromSeconds(10),
            MongoDbConnectTimeout         = TimeSpan.FromSeconds(5),
            MongoDbMinConnectionPoolSize  = 1,
            MongoDbMaxConnectionPoolSize  = 100,
            MaxConnectionIdleTime         = TimeSpan.FromMinutes(10),
            MaxConnectionLifeTime         = TimeSpan.FromHours(1),
            MongoDbQueryTimeout           = TimeSpan.FromMinutes(2),
            MongoDbPingTimeoutSec         = 60,
            MongoDbQueryBatchSize         = 5000,
            MongoDbConnectionRetries      = 3,
            MongoDbRetryTimeoutSec        = 5
        };

        // Act
        MongoDbConfig.CopyFrom(configRecord, settings);

        // Assert
        Assert.AreEqual(configRecord.MongoDbUrl,                MongoDbConfig.MongoDbUrl);
        Assert.AreEqual(configRecord.MongoDbDatabase,           MongoDbConfig.MongoDbDatabase);
        Assert.AreEqual(configRecord.Auth.User,                 MongoDbConfig.MongoDbUser);
        Assert.AreEqual(configRecord.Auth.Password,             MongoDbConfig.MongoDbPassword);
        Assert.AreEqual(configRecord.Auth.AuthDatabase,         MongoDbConfig.MongoDbAuthDatabase);
        Assert.AreEqual(configRecord.Auth.Method,               MongoDbConfig.MongoDbAuthMethod);
        Assert.AreEqual(configRecord.AdminAuth.User,            MongoDbConfig.MongoDbAdminUser);
        Assert.AreEqual(configRecord.AdminAuth.Password,        MongoDbConfig.MongoDbAdminPassword);
        Assert.AreEqual(configRecord.AdminAuth.AuthDatabase,    MongoDbConfig.MongoDbAdminDatabase);
        Assert.AreEqual(configRecord.DirectConnection,          MongoDbConfig.MongoDbDirectConnection);
        Assert.AreEqual(configRecord.UseTls,                    MongoDbConfig.MongoDbUseTls);
        Assert.AreEqual(configRecord.AllowShardAccess,          MongoDbConfig.MongoDbAllowShardAccess);
        Assert.AreEqual(settings.MongoDbSocketTimeout,          MongoDbConfig.MongoDbSocketTimeout);
        Assert.AreEqual(settings.MongoDbServerSelectionTimeout, MongoDbConfig.MongoDbServerSelectionTimeout);
        Assert.AreEqual(settings.MongoDbConnectTimeout,         MongoDbConfig.MongoDbConnectTimeout);
        Assert.AreEqual(settings.MongoDbMinConnectionPoolSize,  MongoDbConfig.MongoDbMinConnectionPoolSize);
        Assert.AreEqual(settings.MongoDbMaxConnectionPoolSize,  MongoDbConfig.MongoDbMaxConnectionPoolSize);
        Assert.AreEqual(settings.MaxConnectionIdleTime,         MongoDbConfig.MongoDbMaxConnectionIdleTime);
        Assert.AreEqual(settings.MaxConnectionLifeTime,         MongoDbConfig.MongoDbMaxConnectionLifeTime);
        Assert.AreEqual(settings.MongoDbQueryTimeout,           MongoDbConfig.MongoDbQueryTimeout);
        Assert.AreEqual(settings.MongoDbPingTimeoutSec,         MongoDbConfig.MongoDbPingTimeoutSec);
        Assert.AreEqual(settings.MongoDbQueryBatchSize,         MongoDbConfig.MongoDbQueryBatchSize);
        Assert.AreEqual(settings.MongoDbConnectionRetries,      MongoDbConfig.MongoDbConnectionRetries);
        Assert.AreEqual(settings.MongoDbRetryTimeoutSec,        MongoDbConfig.MongoDbRetryTimeoutSec);
    }
}