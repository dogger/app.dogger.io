using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Ioc;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Auth0
{
    [TestClass]
    public class GetAuth0UserFromEmailsQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoEmailsProvided_ThrowsException()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var handler = new GetAuth0UserFromEmailsQueryHandler(fakeManagementApiClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await handler.Handle(
                    new GetAuth0UserFromEmailsQuery(Array.Empty<string>()),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SeveralEmailsGivenWithOneMatch_MatchingUserReturned()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var fakeManagementApiClient = await fakeManagementApiClientFactory.Value.CreateAsync();
            fakeManagementApiClient
                .GetUsersByEmailAsync("matching@example.com")
                .Returns(new List<User>()
                {
                    new User()
                    {
                        UserName = "matching"
                    }
                });

            var handler = new GetAuth0UserFromEmailsQueryHandler(fakeManagementApiClientFactory);

            //Act
            var user = await handler.Handle(
                new GetAuth0UserFromEmailsQuery(new []
                {
                    "non-matching-1@example.com",
                    "matching@example.com",
                    "non-matching-2@example.com"
                }),
                default);

            //Assert
            Assert.IsNotNull(user);
            Assert.AreEqual("matching", user.UserName);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SeveralEmailsGivenWithNoMatch_ReturnsNull()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var handler = new GetAuth0UserFromEmailsQueryHandler(fakeManagementApiClientFactory);

            //Act
            var user = await handler.Handle(
                new GetAuth0UserFromEmailsQuery(new[]
                {
                    "non-matching-1@example.com",
                    "non-matching-2@example.com"
                }),
                default);

            //Assert
            Assert.IsNull(user);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConditions_DisposesClient()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IOptionalService<IManagementApiClientFactory>>();

            var fakeManagementApiClient = await fakeManagementApiClientFactory.Value.CreateAsync();

            var handler = new GetAuth0UserFromEmailsQueryHandler(fakeManagementApiClientFactory);

            //Act
            await handler.Handle(
                new GetAuth0UserFromEmailsQuery(new[]
                {
                    "non-matching-1@example.com",
                    "non-matching-2@example.com"
                }),
                default);

            //Assert
            fakeManagementApiClient
                .Received(1)
                .Dispose();
        }
    }
}
