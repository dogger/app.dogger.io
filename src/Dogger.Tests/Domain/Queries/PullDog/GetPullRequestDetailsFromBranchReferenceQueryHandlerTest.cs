using System;
using System.Threading.Tasks;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromBranchReference;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.GitHub.Octokit;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using Serilog;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetPullRequestDetailsFromBranchReferenceQueryHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstallationIdNotFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var handler = new GetPullRequestDetailsFromBranchReferenceQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new GetPullRequestDetailsFromBranchReferenceQuery(
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
                .Repository
                .Get(1338)
                .Returns(new RepositoryBuilder()
                    .WithUser(new UserBuilder()
                        .WithLogin("some-login"))
                    .WithName("some-repository-name"));

            fakeGitHubClient
                .Search
                .SearchIssues(Arg.Is<SearchIssuesRequest>(args =>
                    args.Term == "head:some-branch-reference type:pr state:open repo:some-login/some-repository-name"))
                .Returns(new SearchIssuesResult(1, false, new[]
                {
                    new IssueBuilder()
                        .WithPullRequest(new PullRequest(1339))
                        .Build()
                }));

            var handler = new GetPullRequestDetailsFromBranchReferenceQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var pullRequest = await handler.Handle(
                new GetPullRequestDetailsFromBranchReferenceQuery(
                    new TestPullDogRepositoryBuilder()
                        .WithHandle("1338")
                        .WithGitHubInstallationId(1337),
                    "some-branch-reference"),
                default);

            //Assert
            Assert.AreEqual(1339, pullRequest.Number);
        }
    }
}
