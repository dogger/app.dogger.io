using System.Net;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog.GitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Octokit;
using Serilog;
using User = Octokit.User;

namespace Dogger.Tests.Domain.Services.PullDog
{
    [TestClass]
    public class GitHubPullDogRepositoryClientTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetPullRequestDetails_PullRequestGiven_ReturnsProperFullNameFromReference()
        {
            //Arrange
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var client = new GitHubPullDogRepositoryClient(
                fakeGitHubClient,
                Substitute.For<ILogger>(),
                new GitReference(
                    default,
                    default,
                    default,
                    "some-reference",
                    default,
                    default,
                    new Repository(
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
                            "some-user-name",
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
                        default,
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
                        default)));

            //Act
            var details = client.GetPullRequestDetails(new PullDogPullRequest()
            {
                Handle = "some-handle"
            });

            //Assert
            Assert.IsNotNull(details);
            Assert.AreEqual("some-repository-name", details.RepositoryFullName);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetPullRequestDetails_PullRequestGiven_ReturnsProperLink()
        {
            //Arrange
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            var client = new GitHubPullDogRepositoryClient(
                fakeGitHubClient,
                Substitute.For<ILogger>(),
                new GitReference(
                    default,
                    default,
                    default,
                    "some-reference",
                    default,
                    default,
                    new Repository(
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
                            "some-user-name",
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
                        default,
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
                        default)));

            //Act
            var details = client.GetPullRequestDetails(new PullDogPullRequest()
            {
                Handle = "some-handle"
            });

            //Assert
            Assert.IsNotNull(details);
            Assert.AreEqual("https://github.com/some-repository-name/pull/some-handle", details.PullRequestLink);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetFileContents_NonExistingPathGiven_ReturnsEmptyArray()
        {
            //Arrange
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Repository
                .Content
                .GetAllContentsByRef(
                    "some-user-name",
                    "some-repository-name",
                    "some-path",
                    "some-reference")
                .Throws(new NotFoundException("dummy", HttpStatusCode.NotFound));

            var client = new GitHubPullDogRepositoryClient(
                fakeGitHubClient,
                Substitute.For<ILogger>(),
                new GitReference(
                    default,
                    default,
                    default,
                    "some-reference",
                    default,
                    default,
                    new Repository(
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
                            "some-user-name",
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
                        default)));

            //Act
            var contents = await client.GetFilesForPathAsync("some-path");

            //Assert
            Assert.IsNotNull(contents);
            Assert.AreEqual(0, contents.Length);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetFileContents_ValidPathGiven_FetchesAllFileContents()
        {
            //Arrange
            var fakeGitHubClient = Substitute.For<IGitHubClient>();

            fakeGitHubClient
                .Repository
                .Content
                .GetAllContentsByRef(
                    "some-user-name",
                    "some-repository-name",
                    "some-path",
                    "some-reference")
                .Returns(new []
                {
                    new RepositoryContent(
                        default,
                        default,
                        default,
                        default,
                        ContentType.File,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default),
                    new RepositoryContent(
                        default,
                        default,
                        default,
                        default,
                        ContentType.File,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default,
                        default)
                });

            var client = new GitHubPullDogRepositoryClient(
                fakeGitHubClient,
                Substitute.For<ILogger>(),
                new GitReference(
                    default,
                    default,
                    default,
                    "some-reference",
                    default,
                    default,
                    new Repository(
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
                            "some-user-name",
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
                        default)));

            //Act
            var contents = await client.GetFilesForPathAsync("some-path");

            //Assert
            Assert.IsNotNull(contents);
            Assert.AreEqual(2, contents.Length);
        }
    }
}
