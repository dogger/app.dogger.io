using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest;
using Dogger.Domain.Models;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.Domain.Models;
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
                        new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithGitHubInstallationId(null)),
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
                        new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("invalid-handle")
                                .WithGitHubInstallationId(1337)),
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
                        new TestPullDogPullRequestBuilder()
                            .WithHandle("invalid-handle")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("1337")
                                .WithGitHubInstallationId(1337)),
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
                    new TestPullDogPullRequestBuilder()
                        .WithHandle("1337")
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("1337")
                            .WithGitHubInstallationId(1337)),
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
