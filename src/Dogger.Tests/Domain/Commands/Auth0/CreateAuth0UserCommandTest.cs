using System;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Dogger.Domain.Commands.Auth0.CreateAuth0User;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Ioc;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Commands.Auth0
{
    [TestClass]
    public class CreateAuth0UserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoEmailsGiven_ThrowsException()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var handler = new CreateAuth0UserCommandHandler(
                fakeManagementApiClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(new CreateAuth0UserCommand(Array.Empty<string>()), default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ThreeEmailsGiven_CreatesThreeUsersLinkedIntoOne()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var fakeManagementApiClient = await fakeManagementApiClientFactory.Value.CreateAsync();

            var firstCreatedUser = new User()
            {
                UserId = "user-1"
            };

            fakeManagementApiClient
                .CreateUserAsync(Arg.Any<UserCreateRequest>())
                .Returns(
                    firstCreatedUser,
                    new User()
                    {
                        UserId = "user-2"
                    },
                    new User()
                    {
                        UserId = "user-3"
                    });

            var handler = new CreateAuth0UserCommandHandler(
                fakeManagementApiClientFactory);

            //Act
            var createdUser = await handler.Handle(
                new CreateAuth0UserCommand(new[]
                {
                    "some-email-1@example.com",
                    "some-email-2@example.com",
                    "some-email-3@example.com"
                }),
                default);

            //Assert
            Assert.AreSame(firstCreatedUser, createdUser);

            await fakeManagementApiClient
                .Received(2)
                .LinkUserAccountAsync(
                    "user-1",
                    Arg.Any<UserAccountLinkRequest>());

            await fakeManagementApiClient
                .Received(1)
                .LinkUserAccountAsync(
                    "user-1",
                    Arg.Is<UserAccountLinkRequest>(args =>
                        args.UserId == "user-2"));

            await fakeManagementApiClient
                .Received(1)
                .LinkUserAccountAsync(
                    "user-1",
                    Arg.Is<UserAccountLinkRequest>(args =>
                        args.UserId == "user-3"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ThreeEmailsGivenAndOneCreationFails_DeletesAdditionalCreatedUser()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var fakeManagementApiClient = await fakeManagementApiClientFactory.Value.CreateAsync();

            var firstCreatedUser = new User()
            {
                UserId = "user-1"
            };

            fakeManagementApiClient
                .CreateUserAsync(Arg.Any<UserCreateRequest>())
                .Returns(
                    firstCreatedUser,
                    new User()
                    {
                        UserId = "user-2"
                    },
                    new User()
                    {
                        UserId = "user-3"
                    });

            fakeManagementApiClient
                .LinkUserAccountAsync(
                    "user-1",
                    Arg.Is<UserAccountLinkRequest>(args =>
                        args.UserId == "user-3"))
                .Throws(new TestException());

            var handler = new CreateAuth0UserCommandHandler(
                fakeManagementApiClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await handler.Handle(
                    new CreateAuth0UserCommand(new[]
                    {
                        "some-email-1@example.com",
                        "some-email-2@example.com",
                        "some-email-3@example.com"
                    }),
                    default));

            //Assert
            Assert.IsNotNull(exception);

            await fakeManagementApiClient
                .Received(2)
                .LinkUserAccountAsync(
                    "user-1",
                    Arg.Any<UserAccountLinkRequest>());

            await fakeManagementApiClient
                .Received(1)
                .DeleteUserAsync(Arg.Any<string>());

            await fakeManagementApiClient
                .Received(1)
                .DeleteUserAsync(
                    "user-3");
        }
    }
}
