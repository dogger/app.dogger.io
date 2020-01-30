using System;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.Webhooks
{
    [TestClass]
    public class ConfigurationCommitPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_BranchNotMasterBranch_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Reference = "refs/heads/develop",
                Pusher = new UserPayload(),
                Commits = new[]
                {
                    new CommitPayload()
                    {
                        Added = new [] { "pull-dog.json" },
                        Modified = Array.Empty<string>()
                    }
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_NoCommitsContainingPullDogConfig_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Reference = "refs/heads/master",
                Pusher = new UserPayload(),
                Commits = new[]
                {
                    new CommitPayload()
                    {
                        Added = Array.Empty<string>(),
                        Modified = Array.Empty<string>()
                    }
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ConfigurationFileInCommitAddsAndOnMasterBranch_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Reference = "refs/heads/master",
                Pusher = new UserPayload(),
                Commits = new[]
                {
                    new CommitPayload()
                    {
                        Added = new [] { "pull-dog.json" },
                        Modified = Array.Empty<string>()
                    }
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ConfigurationFileInCommitModifiesAndOnMasterBranch_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Reference = "refs/heads/master",
                Pusher = new UserPayload(),
                Commits = new[]
                {
                    new CommitPayload()
                    {
                        Added = Array.Empty<string>(),
                        Modified = new [] { "pull-dog.json" }
                    }
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConfigurationCommitPayloadWithValidConfigurationFile_ConfiguresPullDogRepository()
        {
            //Arrange
            var settings = new PullDogSettings();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogSettingsByGitHubInstallationIdQuery>(args => args.InstallationId == 1338))
                .Returns(settings);

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayload()
            {
                Pusher = new UserPayload(),
                Installation = new InstallationPayload()
                {
                    Id = 1338
                },
                Repository = new RepositoryPayload()
                {
                    Id = 1337
                }
            });

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<EnsurePullDogRepositoryCommand>(args =>
                    args.PullDogSettings == settings &&
                    args.RepositoryHandle == "1337"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConfigurationCommitPayloadWithNoSettingsPresent_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ConfigurationCommitPayloadHandler(fakeMediator);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.HandleAsync(new WebhookPayload()
                {
                    Pusher = new UserPayload(),
                    Installation = new InstallationPayload()
                    {
                        Id = 1338
                    },
                    Repository = new RepositoryPayload()
                    {
                        Id = 1337
                    }
                }));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}
