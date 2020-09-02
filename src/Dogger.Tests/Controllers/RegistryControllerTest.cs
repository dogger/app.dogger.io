using System;
using System.Threading.Tasks;
using Dogger.Controllers.Registry;
using Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class RegistryControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetLogin_UserAuthenticated_ReturnsLoginCodeForRepositoryWriteUser()
        {
            //Arrange
            var fakeAuthenticatedUserId = Guid.NewGuid();

            var readUser = new TestAmazonUserBuilder().Build();
            var writeUser = new TestAmazonUserBuilder().Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<EnsureUserForIdentityCommand>())
                .Returns(new TestUserBuilder()
                    .WithId(fakeAuthenticatedUserId));

            fakeMediator
                .Send(Arg.Is<EnsureRepositoryWithNameCommand>(args =>
                    args.Name == fakeAuthenticatedUserId.ToString()))
                .Returns(new RepositoryResponse(
                    "some-repository-name",
                    "some-repository-url",
                    readUser,
                    writeUser));

            fakeMediator
                .Send(Arg.Is<GetRepositoryLoginForUserQuery>(args => args.AmazonUser == writeUser))
                .Returns(new RepositoryLoginResponse(
                    "some-username",
                    "some-password"));

            var controller = new RegistryController(fakeMediator);
            controller.FakeAuthentication("dummy");

            //Act
            var loginResponse = await controller.GetLogin();
            var loginObject = loginResponse.ToObject<LoginResponse>();

            //Assert
            Assert.AreEqual("some-repository-url", loginObject.Url);
            Assert.AreEqual("some-username", loginObject.Username);
            Assert.AreEqual("some-password", loginObject.Password);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetLoginForDemo_NoUserAuthenticated_ReturnsLoginCodeForRepositoryWriteUser()
        {
            //Arrange
            var readUser = new TestAmazonUserBuilder().Build();
            var writeUser = new TestAmazonUserBuilder().Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureRepositoryWithNameCommand>(args => args.Name == "demo"))
                .Returns(new RepositoryResponse(
                    "some-repository-name",
                    "some-repository-url",
                    readUser,
                    writeUser));

            fakeMediator
                .Send(Arg.Is<GetRepositoryLoginForUserQuery>(args => args.AmazonUser == writeUser))
                .Returns(new RepositoryLoginResponse(
                    "some-username",
                    "some-password"));

            var controller = new RegistryController(fakeMediator);

            //Act
            var loginResponse = await controller.GetLoginForDemo();
            var loginObject = loginResponse.ToObject<LoginResponse>();

            //Assert
            Assert.AreEqual("some-repository-url", loginObject.Url);
            Assert.AreEqual("some-username", loginObject.Username);
            Assert.AreEqual("some-password", loginObject.Password);
        }
    }
}
