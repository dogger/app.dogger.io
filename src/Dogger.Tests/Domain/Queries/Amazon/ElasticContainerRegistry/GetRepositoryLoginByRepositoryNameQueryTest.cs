using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.ECR;
using Amazon.ECR.Model;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName;
using Dogger.Domain.Services.Amazon.Identity;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Amazon.ElasticContainerRegistry
{
    [TestClass]
    public class GetRepositoryLoginByRepositoryNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AmazonUserGiven_ReturnsCreatedAuthorizationToken()
        {
            //Arrange
            var fakeUserAuthenticatedEcrService = Substitute.For<IAmazonECR>();
            fakeUserAuthenticatedEcrService
                .GetAuthorizationTokenAsync(Arg.Any<GetAuthorizationTokenRequest>())
                .Returns(new GetAuthorizationTokenResponse()
                {
                    AuthorizationData = new List<AuthorizationData>()
                    {
                        new AuthorizationData()
                        {
                            AuthorizationToken = Convert.ToBase64String(
                                Encoding.UTF8.GetBytes("username:password"))
                        }
                    }
                });

            var fakeUserAuthenticatedEcrServiceFactory = Substitute.For<IUserAuthenticatedServiceFactory<IAmazonECR>>();
            fakeUserAuthenticatedEcrServiceFactory
                .CreateAsync("some-amazon-username")
                .Returns(fakeUserAuthenticatedEcrService);

            var handler = new GetRepositoryLoginByRepositoryNameQueryHandler(
                fakeUserAuthenticatedEcrServiceFactory);

            //Act
            var loginResponse = await handler.Handle(
                new GetRepositoryLoginForUserQuery(new TestAmazonUserBuilder()

                    .WithName("some-amazon-username")),
                default);

            //Assert
            Assert.AreEqual("username", loginResponse.Username);
            Assert.AreEqual("password", loginResponse.Password);

            await fakeUserAuthenticatedEcrServiceFactory
                .Received(1)
                .CreateAsync("some-amazon-username");

            await fakeUserAuthenticatedEcrService
                .Received(1)
                .GetAuthorizationTokenAsync(Arg.Any<GetAuthorizationTokenRequest>());
        }
    }
}
