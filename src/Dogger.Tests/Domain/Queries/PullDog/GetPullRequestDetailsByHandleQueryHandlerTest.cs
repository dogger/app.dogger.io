﻿using System;
using System.Net;
using System.Threading.Tasks;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using Serilog;

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

            var handler = new GetPullRequestDetailsByHandleQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new GetPullRequestDetailsByHandleQuery(
                        new TestPullDogRepositoryBuilder(),
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

            var handler = new GetPullRequestDetailsByHandleQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var pullRequest = await handler.Handle(
                new GetPullRequestDetailsByHandleQuery(
                    new TestPullDogRepositoryBuilder()
                        .WithHandle("1338")
                        .WithGitHubInstallationId(1337)
,
                    "1339"),
                default);

            //Assert
            Assert.AreEqual(1339, pullRequest.Number);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestNotFound_ReturnsNull()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(1338, 1339)
                .Throws(new NotFoundException(
                    "dummy", 
                    HttpStatusCode.NotFound));

            var handler = new GetPullRequestDetailsByHandleQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var pullRequest = await handler.Handle(
                new GetPullRequestDetailsByHandleQuery(
                    new TestPullDogRepositoryBuilder()
                        .WithHandle("1338")
                        .WithGitHubInstallationId(1337)
                    ,
                    "1339"),
                default);

            //Assert
            Assert.IsNull(pullRequest);
        }
    }
}
