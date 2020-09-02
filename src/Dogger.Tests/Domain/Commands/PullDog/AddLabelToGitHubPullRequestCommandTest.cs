using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.AddLabelToGitHubPullRequest;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class AddLabelToGitHubPullRequestCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GitHubInstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new AddLabelToGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new AddLabelToGitHubPullRequestCommand(
                        new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithGitHubInstallationId(null)),
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

            var handler = new AddLabelToGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new AddLabelToGitHubPullRequestCommand(
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

            var handler = new AddLabelToGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new AddLabelToGitHubPullRequestCommand(
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

            var handler = new AddLabelToGitHubPullRequestCommandHandler(
                fakeGitHubClientFactory);

            //Act
            await handler.Handle(
                new AddLabelToGitHubPullRequestCommand(
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
                .AddToIssue(
                    1337,
                    1337,
                    Arg.Is<string[]>(args => args.Single() == "some-label"));
        }
    }
}
