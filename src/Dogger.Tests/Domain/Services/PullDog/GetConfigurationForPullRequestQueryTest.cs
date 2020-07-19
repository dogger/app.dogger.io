using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.PullDog
{
    [TestClass]
    public class GetConfigurationForPullRequestQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationFileNotPresent_ReturnsDefaultConfiguration()
        {
            //Arrange
            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var handler = new GetConfigurationForPullRequestQueryHandler(
                fakePullDogFileCollectorFactory);

            //Act
            var configuration = await handler.Handle(
                new GetConfigurationForPullRequestQuery(
                    new PullDogPullRequest()),
                default);

            //Assert
            Assert.IsNotNull(configuration?.DockerComposeYmlFilePaths);
            Assert.AreEqual("docker-compose.yml", configuration.DockerComposeYmlFilePaths[0]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationFileOverrideNotPresent_ReturnsOriginalConfiguration()
        {
            //Arrange
            var fakePullDogPullRequest = new PullDogPullRequest()
            {
                ConfigurationOverride = null
            };

            var fakeConfiguration = new ConfigurationFile(Array.Empty<string>());

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogFileCollector = await fakePullDogFileCollectorFactory.CreateAsync(fakePullDogPullRequest);
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns(fakeConfiguration);

            var handler = new GetConfigurationForPullRequestQueryHandler(
                fakePullDogFileCollectorFactory);

            //Act
            var configuration = await handler.Handle(
                new GetConfigurationForPullRequestQuery(
                    fakePullDogPullRequest),
                default);

            //Assert
            Assert.AreSame(fakeConfiguration, configuration);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationOverridePresentWithBuildArguments_OverridesExistingBuildArguments()
        {
            //Arrange
            var fakePullDogPullRequest = new PullDogPullRequest()
            {
                ConfigurationOverride = new ConfigurationFileOverride()
                {
                    BuildArguments = new Dictionary<string, string>()
                    {
                        { "some-new-key", "some-new-value"}
                    }
                }
            };

            var fakeConfiguration = new ConfigurationFile(Array.Empty<string>())
            {
                BuildArguments = new Dictionary<string, string>()
                {
                    { "some-old-key", "some-old-value"}
                }
            };

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogFileCollector = await fakePullDogFileCollectorFactory.CreateAsync(fakePullDogPullRequest);
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns(fakeConfiguration);

            var handler = new GetConfigurationForPullRequestQueryHandler(
                fakePullDogFileCollectorFactory);

            //Act
            var configuration = await handler.Handle(
                new GetConfigurationForPullRequestQuery(
                    fakePullDogPullRequest),
                default);

            //Assert
            var buildArgument = configuration.BuildArguments!.Single();
            Assert.AreEqual("some-new-key", buildArgument.Key);
            Assert.AreEqual("some-new-value", buildArgument.Value);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationOverridePresentWithConversationMode_OverridesExistingConversationMode()
        {
            //Arrange
            var fakePullDogPullRequest = new PullDogPullRequest()
            {
                ConfigurationOverride = new ConfigurationFileOverride()
                {
                    ConversationMode = ConversationMode.MultipleComments
                }
            };

            var fakeConfiguration = new ConfigurationFile(Array.Empty<string>())
            {
                ConversationMode = ConversationMode.SingleComment
            };

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogFileCollector = await fakePullDogFileCollectorFactory.CreateAsync(fakePullDogPullRequest);
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns(fakeConfiguration);

            var handler = new GetConfigurationForPullRequestQueryHandler(
                fakePullDogFileCollectorFactory);

            //Act
            var configuration = await handler.Handle(
                new GetConfigurationForPullRequestQuery(
                    fakePullDogPullRequest),
                default);

            //Assert
            Assert.AreEqual(ConversationMode.MultipleComments, configuration.ConversationMode);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ConfigurationOverridePresentWithExpiry_OverridesExistingExpiry()
        {
            //Arrange
            var fakePullDogPullRequest = new PullDogPullRequest()
            {
                ConfigurationOverride = new ConfigurationFileOverride()
                {
                    Expiry = TimeSpan.FromMinutes(2)
                }
            };

            var fakeConfiguration = new ConfigurationFile(Array.Empty<string>())
            {
                Expiry = TimeSpan.FromMinutes(1)
            };

            var fakePullDogFileCollectorFactory = Substitute.For<IPullDogFileCollectorFactory>();

            var fakePullDogFileCollector = await fakePullDogFileCollectorFactory.CreateAsync(fakePullDogPullRequest);
            fakePullDogFileCollector
                .GetConfigurationFileAsync()
                .Returns(fakeConfiguration);

            var handler = new GetConfigurationForPullRequestQueryHandler(
                fakePullDogFileCollectorFactory);

            //Act
            var configuration = await handler.Handle(
                new GetConfigurationForPullRequestQuery(
                    fakePullDogPullRequest),
                default);

            //Assert
            Assert.AreEqual(TimeSpan.FromMinutes(2), configuration.Expiry);
        }
    }
}
