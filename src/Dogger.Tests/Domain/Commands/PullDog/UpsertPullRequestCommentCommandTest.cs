using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.GitHub.Octokit;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class UpsertPullRequestCommentCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GitHubInstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                Substitute.For<IMediator>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new UpsertPullRequestCommentCommand(
                        new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithGitHubInstallationId(null))
                            .Build(),
                        "some-content"),
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

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                Substitute.For<IMediator>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new UpsertPullRequestCommentCommand(
                        new TestPullDogPullRequestBuilder()
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("invalid-handle")
                                .WithGitHubInstallationId(1337))
                            .Build(),
                        "some-content"),
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

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                Substitute.For<IMediator>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new UpsertPullRequestCommentCommand(
                        new TestPullDogPullRequestBuilder()
                            .WithHandle("invalid-handle")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithHandle("1337")
                                .WithGitHubInstallationId(1337))
                            .Build(),
                        "some-content"),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingBotCommentFoundAndConversationModeIsSingleComment_UpdatesExistingComment()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(
                    1337,
                    1337)
                .Returns(
                    CreatePullRequestDto(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1337))));

            fakeGitHubClient
                .Issue
                .Comment
                .GetAllForIssue(
                    Arg.Any<long>(),
                    Arg.Any<int>())
                .Returns(new[]
                {
                    CreateIssueDto(CreateUserDto(1337)),
                    CreateIssueDto(CreateUserDto(64123634)),
                    CreateIssueDto(CreateUserDto(1338))
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>())
                {
                    ConversationMode = ConversationMode.SingleComment
                });

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                fakeMediator);

            //Act
            await handler.Handle(
                new UpsertPullRequestCommentCommand(
                    new TestPullDogPullRequestBuilder()
                        .WithHandle("1337")
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("1337")
                            .WithGitHubInstallationId(1337))
                        .Build(),
                    "some-content"),
                default);

            //Assert
            await fakeGitHubClient
                .Issue
                .Comment
                .Received(1)
                .Update(
                    1337,
                    Arg.Any<int>(),
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingBotCommentFoundAndConversationModeIsMultipleComments_UpdatesExistingComment()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(
                    1337,
                    1337)
                .Returns(
                    CreatePullRequestDto(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1337))));

            fakeGitHubClient
                .Issue
                .Comment
                .GetAllForIssue(
                    Arg.Any<long>(),
                    Arg.Any<int>())
                .Returns(new[]
                {
                    CreateIssueDto(CreateUserDto(1337)),
                    CreateIssueDto(CreateUserDto(64123634)),
                    CreateIssueDto(CreateUserDto(1338))
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile(new List<string>())
                {
                    ConversationMode = ConversationMode.MultipleComments
                });

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                fakeMediator);

            //Act
            await handler.Handle(
                new UpsertPullRequestCommentCommand(
                    new TestPullDogPullRequestBuilder()
                        .WithHandle("1337")
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("1337")
                            .WithGitHubInstallationId(1337))
                        .Build(),
                    "some-content"),
                default);

            //Assert
            await fakeGitHubClient
                .Issue
                .Comment
                .Received(1)
                .Create(
                    1337,
                    Arg.Any<int>(),
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ExistingBotCommentFoundAndConversationModeNotSet_UpdatesExistingComment()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(
                    1337,
                    1337)
                .Returns(
                    CreatePullRequestDto(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1337))));

            fakeGitHubClient
                .Issue
                .Comment
                .GetAllForIssue(
                    Arg.Any<long>(),
                    Arg.Any<int>())
                .Returns(new[]
                {
                    CreateIssueDto(CreateUserDto(1337)),
                    CreateIssueDto(CreateUserDto(64123634)),
                    CreateIssueDto(CreateUserDto(1338))
                });

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                Substitute.For<IMediator>());

            //Act
            await handler.Handle(
                new UpsertPullRequestCommentCommand(
                    new TestPullDogPullRequestBuilder()
                        .WithHandle("1337")
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("1337")
                            .WithGitHubInstallationId(1337))
                        .Build(),
                    "some-content"),
                default);

            //Assert
            await fakeGitHubClient
                .Issue
                .Comment
                .Received(1)
                .Update(
                    1337,
                    Arg.Any<int>(),
                    Arg.Any<string>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoExistingBotCommentFound_InsertsNewComment()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(
                    1337,
                    1337)
                .Returns(
                    CreatePullRequestDto(
                        CreateGitReferenceDto(
                            CreateRepositoryDto(
                                1337))));

            fakeGitHubClient
                .Issue
                .Comment
                .GetAllForIssue(
                    Arg.Any<long>(),
                    Arg.Any<int>())
                .Returns(new[]
                {
                    CreateIssueDto(CreateUserDto(1337)),
                    CreateIssueDto(CreateUserDto(1338))
                });

            var handler = new UpsertPullRequestCommentCommandHandler(
                fakeGitHubClientFactory,
                Substitute.For<IMediator>());

            //Act
            await handler.Handle(
                new UpsertPullRequestCommentCommand(
                    new TestPullDogPullRequestBuilder()
                        .WithHandle("1337")
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithHandle("1337")
                            .WithGitHubInstallationId(1337))
                        .Build(),
                    "some-content"),
                default);

            //Assert
            await fakeGitHubClient
                .Issue
                .Comment
                .Received(1)
                .Create(
                    1337,
                    Arg.Any<int>(),
                    Arg.Any<string>());
        }

        private static IssueComment CreateIssueDto(
            Octokit.User user)
        {
            return new IssueComment(
                default,
                default,
                default,
                default,
                default,
                default,
                default,
                user,
                default,
                default);
        }

        private static Octokit.User CreateUserDto(int id)
        {
            return new UserBuilder()
                .WithId(id)
                .Build();
        }

        private static Repository CreateRepositoryDto(
            long id)
        {
            return new RepositoryBuilder()
                .WithId(id)
                .Build();
        }

        private static GitReference CreateGitReferenceDto(
            Repository repository)
        {
            return new GitReference(
                default,
                default,
                default,
                default,
                default,
                default,
                repository);
        }

        private static PullRequest CreatePullRequestDto(GitReference @base)
        {
            return new PullRequestBuilder()
                .WithBase(@base)
                .Build();
        }
    }
}
