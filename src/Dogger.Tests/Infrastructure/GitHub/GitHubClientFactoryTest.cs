using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Flurl;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using Serilog;

namespace Dogger.Tests.Infrastructure.GitHub
{
    [TestClass]
    public class GitHubClientFactoryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CreateInstallationClient_NoInstallationIdProvided_ThrowsException()
        {
            //Arrange
            var gitHubClientFactory = new GitHubClientFactory(
                Substitute.For<IGitHubClient>(),
                Substitute.For<IFlurlClientFactory>(),
                Substitute.For<ILogger>(),
                Substitute.For<IOptionsMonitor<GitHubOptions>>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await gitHubClientFactory.CreateInstallationClientAsync(0));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CreateInstallationClient_InstallationIdProvided_CreatesNewGitHubClientWithCreatedInstallationToken()
        {
            //Arrange
            var fakeGitHubClient = Substitute.For<IGitHubClient>();
            fakeGitHubClient
                .GitHubApps
                .CreateInstallationToken(1337)
                .Returns(new AccessToken("some-token", DateTimeOffset.UtcNow));

            var gitHubClientFactory = new GitHubClientFactory(
                fakeGitHubClient,
                Substitute.For<IFlurlClientFactory>(),
                Substitute.For<ILogger>(),
                Substitute.For<IOptionsMonitor<GitHubOptions>>());

            //Act
            var client = await gitHubClientFactory.CreateInstallationClientAsync(1337);

            //Assert
            Assert.IsNotNull(client);

            await fakeGitHubClient
                .GitHubApps
                .Received(1)
                .CreateInstallationToken(1337);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CreateInstallationInitiatorClient_NoCodeProvided_ThrowsException()
        {
            //Arrange
            var gitHubClientFactory = new GitHubClientFactory(
                Substitute.For<IGitHubClient>(),
                Substitute.For<IFlurlClientFactory>(),
                Substitute.For<ILogger>(),
                Substitute.For<IOptionsMonitor<GitHubOptions>>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await gitHubClientFactory.CreateInstallationInitiatorClientAsync(string.Empty));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CreateInstallationInitiatorClient_NoPullDogOptionsFound_ThrowsException()
        {
            //Arrange
            var gitHubClientFactory = new GitHubClientFactory(
                Substitute.For<IGitHubClient>(),
                Substitute.For<IFlurlClientFactory>(),
                Substitute.For<ILogger>(),
                Substitute.For<IOptionsMonitor<GitHubOptions>>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await gitHubClientFactory.CreateInstallationInitiatorClientAsync("dummy"));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CreateInstallationInitiatorClient_ValidConditions_CreatesNewGitHubClientWithExchangedUserToken()
        {
            //Arrange
            var fakeFlurlClientFactory = Substitute.For<IFlurlClientFactory>();
            var fakeFlurlClient = fakeFlurlClientFactory.Get("https://github.com/login/oauth/access_token");

            var fakeFlurlRequest = fakeFlurlClient.Request();
            fakeFlurlRequest
                .Url
                .Returns(new Url("dummy"));

            fakeFlurlRequest
                .SendAsync(
                    HttpMethod.Post,
                    Arg.Any<StringContent>(),
                    default,
                    HttpCompletionOption.ResponseContentRead)
                .Returns(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("access_token=some-token&expiry=now")
                });

            var fakeGitHubOptionsMonitor = Substitute.For<IOptionsMonitor<GitHubOptions>>();
            fakeGitHubOptionsMonitor
                .CurrentValue
                .Returns(new GitHubOptions()
                {
                    PullDog = new GitHubPullDogOptions()
                });

            var gitHubClientFactory = new GitHubClientFactory(
                Substitute.For<IGitHubClient>(),
                fakeFlurlClientFactory,
                Substitute.For<ILogger>(),
                fakeGitHubOptionsMonitor);

            //Act
            var gitHubClient = await gitHubClientFactory.CreateInstallationInitiatorClientAsync("dummy");

            //Assert
            Assert.IsNotNull(gitHubClient);
        }
    }
}
