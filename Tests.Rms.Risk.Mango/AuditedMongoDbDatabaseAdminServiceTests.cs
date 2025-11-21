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
//using Microsoft.Extensions.Options;
//using Rms.Risk.Mango;
//using Rms.Risk.Mango.Services.Context;
//using Rms.Service.Bootstrap.Security;

//namespace Tests.Rms.Risk.Mango;

//[TestFixture]
//public class AuditedMongoDbDatabaseAdminServiceTests
//{
//    private Mock<IMongoDbDatabaseAdminService>  _mockMongoDbAdminService   = null!;
//    private Mock<IAuditService>                 _mockAuditService          = null!;
//    private Mock<UserSession>                   _mockUserSession           = null!;
//    private UserService                         _userService           = null!;
//    private AuditedMongoDbDatabaseAdminService  _service                   = null!;
//    private Mock<IMongoDbServiceFactory>        _mockMongoDbServiceFactory = null!;
//    private Mock<IChangeNumberChecker>          _mockChangeNumberChecker   = null!;
//    private Mock<IDatabaseConfigurationService> _mockDatabases             = null!;
//    private Mock<IServerSideTokenStore>         _mockServerSideTokenStore = null!;
//    private UserSession                         _userSession                 = null!;

//    [SetUp]
//    public void SetUp()
//    {
//        var mockConfig             = new Mock<MongoDbConfigRecord>();
//        var mockSettings           = new Mock<MongoDbSettings>();
//        _mockMongoDbAdminService   = new Mock<IMongoDbDatabaseAdminService>();
//        _mockAuditService          = new Mock<IAuditService>();
//        _mockServerSideTokenStore  = new Mock<IServerSideTokenStore>();
//        _userService               = new UserService(_mockServerSideTokenStore.Object);
//        _mockMongoDbServiceFactory = new Mock<IMongoDbServiceFactory>();
//        _mockChangeNumberChecker   = new Mock<IChangeNumberChecker>();
//        _mockDatabases             = new Mock<IDatabaseConfigurationService>();

//        _userSession         = new UserSession(
//            mockSettings.Object, // Replace with actual user service mock if needed
//            null!,
//            _userService,
//            _mockMongoDbServiceFactory .Object,
//            _mockChangeNumberChecker   .Object,
//            _mockDatabases             .Object
//            );

//        _service = new AuditedMongoDbDatabaseAdminService(
//            mockConfig.Object,
//            mockSettings.Object,
//            true,
//            _mockUserSession.Object,
//            _mockAuditService.Object
//        );
//    }

//    [Test]
//    public void RunCommand_ShouldRecordAudit_WhenCommandFails()
//    {
//        // Arrange
//        var command           = new BsonDocument("test", "value");
//        var cancellationToken = CancellationToken.None;

//        _mockUserSession.Setup(s => s.HasValidTask()).ReturnsAsync(false);
//        _mockUserSession.Setup(s => s.TaskCheckError).Returns("Invalid task");

//        // Act & Assert
//        var ex = NUnit.Framework.Assert.ThrowsAsync<ApplicationException>(async () =>
//                                                                              await _service.RunCommand(command, cancellationToken)
//        );

//        Assert.AreEqual("Ticket check failed: Invalid task", ex?.Message);
//        _mockAuditService.Verify(a => a.Record(It.IsAny<AuditRecord>(), cancellationToken), Times.Once);
//    }

//    [Test]
//    public async Task RunCommand_ShouldRecordAudit_WhenCommandSucceeds()
//    {
//        // Arrange
//        var command           = new BsonDocument("test", "value");
//        var response          = new BsonDocument { { "ok", true } };
//        var cancellationToken = CancellationToken.None;

//        _mockUserSession.Setup(s => s.HasValidTask()).ReturnsAsync(true);
//        _mockMongoDbAdminService.Setup(m => m.RunCommand(command, cancellationToken)).ReturnsAsync(response);

//        // Act
//        var result = await _service.RunCommand(command, cancellationToken);

//        // Assert
//        Assert.AreEqual(response, result);
//        _mockAuditService.Verify(a => a.Record(It.IsAny<AuditRecord>(), cancellationToken), Times.Once);
//    }

//    [Test]
//    public void RunCommand_ShouldRecordAudit_WhenCommandThrowsException()
//    {
//        // Arrange
//        var command           = new BsonDocument("test", "value");
//        var cancellationToken = CancellationToken.None;

//        _mockUserSession.Setup(s => s.HasValidTask()).ReturnsAsync(true);
//        _mockMongoDbAdminService.Setup(m => m.RunCommand(command, cancellationToken))
//           .ThrowsAsync(new Exception("Command failed"));

//        // Act & Assert
//        var ex = Assert.ThrowsAsync<Exception>(async () =>
//                                                   await _service.RunCommand(command, cancellationToken)
//        );

//        Assert.AreEqual("Command failed", ex?.Message);
//        _mockAuditService.Verify(a => a.Record(It.IsAny<AuditRecord>(), cancellationToken), Times.Once);
//    }
//}