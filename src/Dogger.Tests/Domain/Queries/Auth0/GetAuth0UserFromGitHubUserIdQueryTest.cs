using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Auth0
{
    [TestClass]
    public class GetAuth0UserFromGitHubUserIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GitHubUserIdNotGiven_ThrowsException()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IManagementApiClientFactory>();

            var handler = new GetAuth0UserFromGitHubUserIdQueryHandler(fakeManagementApiClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new GetAuth0UserFromGitHubUserIdQuery(default),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingGitHubUserIdGiven_UserReturned()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IManagementApiClientFactory>();

            var fakeManagementApiClient = await fakeManagementApiClientFactory.CreateAsync();
            fakeManagementApiClient
                .GetAllUsersAsync(
                    Arg.Is<GetUsersRequest>(args =>
                        args.Query == "user_metadata.dogger_github_user_id: 1337"),
                    Arg.Any<PaginationInfo>())
                .Returns(new List<User>()
                {
                    new User()
                });

            var handler = new GetAuth0UserFromGitHubUserIdQueryHandler(fakeManagementApiClientFactory);

            //Act
            var user = await handler.Handle(
                new GetAuth0UserFromGitHubUserIdQuery(1337),
                default);

            //Assert
            Assert.IsNotNull(user);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NonExistingGitHubUserIdGiven_NullReturned()
        {
            //Arrange
            var fakeManagementApiClientFactory = Substitute.For<IManagementApiClientFactory>();

            var handler = new GetAuth0UserFromGitHubUserIdQueryHandler(fakeManagementApiClientFactory);

            //Act
            var user = await handler.Handle(
                new GetAuth0UserFromGitHubUserIdQuery(1337),
                default);

            //Assert
            Assert.IsNull(user);
        }
    }
}
