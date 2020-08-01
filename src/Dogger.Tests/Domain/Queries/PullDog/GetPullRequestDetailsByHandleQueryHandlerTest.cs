﻿using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetPullRequestDetailsByHandleQueryHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new GetPullRequestDetailsByHandleQueryHandler(fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new GetPullRequestDetailsByHandleQuery(
                        new PullDogRepository()
                        {
                            PullDogSettings = new PullDogSettings()
                        }, 
                        "dummy"), 
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestFound_ReturnsPullRequest()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(1338, 1339)
                .Returns(new PullRequest(1339));

            var handler = new GetPullRequestDetailsByHandleQueryHandler(fakeGitHubClientFactory);

            //Act
            var pullRequest = await handler.Handle(
                new GetPullRequestDetailsByHandleQuery(
                    new PullDogRepository()
                    {
                        Handle = "1338",
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    },
                    "1339"), 
                default);

            //Assert
            Assert.AreEqual(1339, pullRequest.Number);
        }
    }
}