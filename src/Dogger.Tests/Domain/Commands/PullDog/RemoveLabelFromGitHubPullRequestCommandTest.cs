﻿using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest;
using Dogger.Domain.Models;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class RemoveLabelFromGitHubPullRequestCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GitHubInstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new RemoveLabelFromGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await handler.Handle(
                    new RemoveLabelFromGitHubPullRequestCommand(
                        new PullDogPullRequest()
                        {
                            PullDogRepository = new PullDogRepository()
                            {
                                GitHubInstallationId = null,
                                PullDogSettings = new PullDogSettings()
                            }
                        },
                        "some-label"), 
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InvalidRepositoryHandleGiven_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new RemoveLabelFromGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new RemoveLabelFromGitHubPullRequestCommand(
                        new PullDogPullRequest()
                        {
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "invalid-handle",
                                GitHubInstallationId = 1337,
                                PullDogSettings = new PullDogSettings()
                            }
                        },
                        "some-label"),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InvalidPullRequestHandleGiven_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new RemoveLabelFromGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new RemoveLabelFromGitHubPullRequestCommand(
                        new PullDogPullRequest()
                        {
                            Handle = "invalid-handle",
                            PullDogRepository = new PullDogRepository()
                            {
                                Handle = "1337",
                                GitHubInstallationId = 1337,
                                PullDogSettings = new PullDogSettings()
                            }
                        },
                        "some-label"),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AllParametersValid_AddsLabelToPullRequest()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);

            var handler = new RemoveLabelFromGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            await handler.Handle(
                new RemoveLabelFromGitHubPullRequestCommand(
                    new PullDogPullRequest()
                    {
                        Handle = "1337",
                        PullDogRepository = new PullDogRepository()
                        {
                            Handle = "1337",
                            GitHubInstallationId = 1337,
                            PullDogSettings = new PullDogSettings()
                        }
                    },
                    "some-label"),
                default);

            //Assert
            await fakeGitHubClient
                .Issue
                .Labels
                .Received(1)
                .RemoveFromIssue(
                    1337,
                    1337,
                    "some-label");
        }
    }
}
