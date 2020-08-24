using System;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonGroupWithName;
using Dogger.Domain.Commands.Amazon.Identity.EnsureAmazonUserWithName;
using Dogger.Domain.Models.Builders;
using Dogger.Infrastructure.Encryption;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Commands.Amazon.Identity
{
    [TestClass]
    public class EnsureAmazonUserWithNameCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingUserFoundInDatabase_ReturnsUser()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var userId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.AmazonUsers.AddAsync(new TestAmazonUserBuilder()

                    .WithName("some-name")
                    .WithId(userId)
                    .Build());
            });

            //Act
            var user = await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name"));

            //Assert
            Assert.IsNotNull(user);
            Assert.AreEqual(userId, user.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoUserFoundInDatabase_AddsUserToDatabase()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            //Act
            var user = await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name"));

            //Assert
            Assert.IsNotNull(user);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var userFromDatabase = await dataContext.AmazonUsers.SingleAsync();
                Assert.IsNotNull(userFromDatabase);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoUserFoundInDatabase_CreatesUserInAmazon()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            //Act
            await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name"));

            //Assert
            await fakeAmazonIdentityManagementService
                .Received(1)
                .CreateUserAsync(Arg.Is<CreateUserRequest>(args =>
                    args.UserName == "some-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoUserFoundInDatabase_CreatesAccessKeyForUser()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            //Act
            await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name"));

            //Assert
            await fakeAmazonIdentityManagementService
                .Received(1)
                .CreateAccessKeyAsync(Arg.Is<CreateAccessKeyRequest>(args =>
                    args.UserName == "some-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoUserFoundInDatabase_SavesCredentialsOnCreatedUser()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                    {
                        SecretAccessKey = "some-secret-access-key",
                        AccessKeyId = "some-access-key-id"
                    }
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            var aesEncryptionHelper = environment.ServiceProvider.GetRequiredService<IAesEncryptionHelper>();

            //Act
            await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var userFromDatabase = await dataContext.AmazonUsers.SingleAsync();
                Assert.AreNotEqual(0, userFromDatabase.EncryptedSecretAccessKey);
                Assert.AreNotEqual(0, userFromDatabase.EncryptedAccessKeyId);

                var decryptedAccessKey = await aesEncryptionHelper.DecryptAsync(userFromDatabase.EncryptedSecretAccessKey);
                var decryptedKeyId = await aesEncryptionHelper.DecryptAsync(userFromDatabase.EncryptedAccessKeyId);
                Assert.AreEqual("some-secret-access-key", decryptedAccessKey);
                Assert.AreEqual("some-access-key-id", decryptedKeyId);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoUserFoundInDatabaseAndUserIdGiven_CreatesGroupForUserId()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonGroupWithNameCommand>())
                .Returns(new Group()
                {
                    GroupName = "some-group"
                });

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonIdentityManagementService);
                    services.AddSingleton(fakeMediator);
                }
            });

            var user = new TestUserBuilder().Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var amazonUser = await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name")
            {
                UserId = user.Id
            });

            //Assert
            Assert.IsNotNull(amazonUser);

            await fakeAmazonIdentityManagementService
                .Received(1)
                .AddUserToGroupAsync(Arg.Is<AddUserToGroupRequest>(args =>
                    args.GroupName == "some-group" &&
                    args.UserName == "some-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ErrorOccuredDuringCreationOfAmazonUser_DoesNothing()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateUserAsync(Arg.Any<CreateUserRequest>())
                .Throws(new TestException());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name")));

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var userFromDatabase = await dataContext.AmazonUsers.SingleOrDefaultAsync();
                Assert.IsNull(userFromDatabase);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ErrorOccuredDuringCreationOfAccessKey_DeletesCreatedAmazonUser()
        {
            //Arrange
            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Throws(new TestException());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeAmazonIdentityManagementService)
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name")));

            //Assert
            Assert.IsNotNull(exception);

            await fakeAmazonIdentityManagementService
                .Received(1)
                .DeleteUserAsync(Arg.Is<DeleteUserRequest>(args => args.UserName == "some-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ErrorOccuredDuringCreationOfGroup_DeletesCreatedAmazonUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonGroupWithNameCommand>())
                .Throws(new TestException());

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonIdentityManagementService);
                    services.AddSingleton(fakeMediator);
                }
            });

            var user = new TestUserBuilder().Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name")
                {
                    UserId = user.Id
                }));

            //Assert
            Assert.IsNotNull(exception);

            await fakeAmazonIdentityManagementService
                .Received(1)
                .DeleteUserAsync(Arg.Is<DeleteUserRequest>(args => args.UserName == "some-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ErrorOccuredDuringCreationOfGroup_DeletesCreatedAmazonUserAccessKey()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureAmazonGroupWithNameCommand>())
                .Throws(new TestException());

            var fakeAmazonIdentityManagementService = Substitute.For<IAmazonIdentityManagementService>();
            fakeAmazonIdentityManagementService
                .CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>())
                .Returns(new CreateAccessKeyResponse()
                {
                    AccessKey = new AccessKey()
                });

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeAmazonIdentityManagementService);
                    services.AddSingleton(fakeMediator);
                }
            });

            var user = new TestUserBuilder().Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(new EnsureAmazonUserWithNameCommand("some-name")
                {
                    UserId = user.Id
                }));

            //Assert
            Assert.IsNotNull(exception);

            await fakeAmazonIdentityManagementService
                .Received(1)
                .DeleteAccessKeyAsync(Arg.Is<DeleteAccessKeyRequest>(args => args.AccessKeyId == "some-name"));
        }
    }
}
