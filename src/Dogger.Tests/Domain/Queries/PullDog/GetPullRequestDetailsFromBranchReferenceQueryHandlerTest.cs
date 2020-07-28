using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromBranchReference;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using Serilog;
using User = Octokit.User;

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
                .Repository
                .Get(1338)
                .Returns(new Repository(
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    new User(
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        "some-login",
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default), 
                    "some-repository-name",
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default,
                    default));

            fakeGitHubClient
                .Search
                .SearchIssues(Arg.Is<SearchIssuesRequest>(args =>
                    args.Term == "head:some-branch-reference type:pr state:open repo:some-login/some-repository-name"))
                .Returns(new SearchIssuesResult(1, false, new[]
                {
                    new Issue(
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        new PullRequest(1339), 
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default)
                }));

            var handler = new GetPullRequestDetailsFromBranchReferenceQueryHandler(
                fakeGitHubClientFactory,
                Substitute.For<ILogger>());

            //Act
            var pullRequest = await handler.Handle(
                new GetPullRequestDetailsFromBranchReferenceQuery(
                    new PullDogRepository()
                    {
                        Handle = "1338",
                        GitHubInstallationId = 1337,
                        PullDogSettings = new PullDogSettings()
                    },
                    "some-branch-reference"),
                default);

            //Assert
            Assert.AreEqual(1339, pullRequest.Number);
        }
    }
}
