using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
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
    public class InstallationConfigurationPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_BranchNotMasterBranch_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

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

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

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
        public async Task CanHandle_ActionIsAdded_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "added"
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionIsRemoved_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "removed"
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConfigurationCommitPayloadWithValidConfigurationFileAndRepositoriesToAdd_AddsRepositoriesToInstallation()
        {
            //Arrange
            var settings = new PullDogSettings();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogSettingsByGitHubInstallationIdQuery>(args => args.InstallationId == 1338))
                .Returns(settings);

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayload()
            {
                Pusher = new UserPayload(),
                Installation = new InstallationPayload()
                {
                    Id = 1338
                },
                RepositoriesAdded = new []
                {
                    new InstallationRepositoryReferencePayload()
                    {
                        Id = 1339
                    },
                    new InstallationRepositoryReferencePayload()
                    {
                        Id = 1340
                    }
                }
            });

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<AddPullDogToGitHubRepositoriesCommand>(args =>
                    args.PullDogSettings == settings &&
                    args.GitHubInstallationId == 1338 &&
                    args.GitHubRepositoryIds.Contains(1339) &&
                    args.GitHubRepositoryIds.Contains(1340)));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConfigurationCommitPayloadWithValidConfigurationFileAndRepositoriesToRemove_RemovesRepositoriesFromInstallation()
        {
            //Arrange
            var settings = new PullDogSettings();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogSettingsByGitHubInstallationIdQuery>(args => args.InstallationId == 1338))
                .Returns(settings);

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayload()
            {
                Pusher = new UserPayload(),
                Installation = new InstallationPayload()
                {
                    Id = 1338
                },
                RepositoriesRemoved = new[]
                {
                    new InstallationRepositoryReferencePayload()
                    {
                        Id = 1339
                    },
                    new InstallationRepositoryReferencePayload()
                    {
                        Id = 1340
                    }
                }
            });

            //Assert
            await fakeMediator
                .Received(2)
                .Send(Arg.Any<DeletePullDogRepositoryCommand>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeletePullDogRepositoryCommand>(args =>
                    args.Handle == "1339"));

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeletePullDogRepositoryCommand>(args =>
                    args.Handle == "1340"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConfigurationCommitPayloadWithNoSettingsPresent_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new InstallationConfigurationPayloadHandler(fakeMediator);

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
