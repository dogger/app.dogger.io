using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog.GitHub;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Domain.Services.PullDog
{
    [TestClass]
    public class GitHubPullDogRepositoryClientFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Create_NoGitHubInstallationIdFound_ThrowsException()
        {
            //Arrange
            var factory = new GitHubPullDogRepositoryClientFactory(
                Substitute.For<IGitHubClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await factory.CreateAsync(new TestPullDogPullRequestBuilder()
                    .WithPullDogRepository(new PullDogRepository()
                    {
                        GitHubInstallationId = null,
                        PullDogSettings = new PullDogSettings()
                    })
                    .Build()));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Create_ValidConditions_ReturnsGitHubPullDogRepositoryClientWithProperParameters()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .PullRequest
                .Get(2, 1)
                .Returns(new PullRequest());

            var factory = new GitHubPullDogRepositoryClientFactory(
                fakeGitHubClientFactory);

            //Act
            var client = await factory.CreateAsync(new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new PullDogRepository()
                {
                    Handle = "2",
                    GitHubInstallationId = 1337,
                    PullDogSettings = new PullDogSettings()
                })
                .Build());

            //Assert
            Assert.IsNotNull(client);

            await fakeGitHubClientFactory
                .Received()
                .CreateInstallationClientAsync(1337);
        }
    }
}