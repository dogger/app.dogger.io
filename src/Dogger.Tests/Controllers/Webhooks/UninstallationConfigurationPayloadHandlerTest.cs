using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.Webhooks
{
    [TestClass]
    public class UninstallationConfigurationPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionIsDeleted_ReturnsTrue()
        {
            //Arrange
            var handler = new UninstallationConfigurationPayloadHandler(
                Substitute.For<IMediator>());

            //Act
            var canHandle = handler.CanHandle(new WebhookPayload()
            {
                Action = "deleted"
            });

            //Assert
            Assert.IsTrue(canHandle);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionNotDeleted_ReturnsFalse()
        {
            //Arrange
            var handler = new UninstallationConfigurationPayloadHandler(
                Substitute.For<IMediator>());

            //Act
            var canHandle = handler.CanHandle(new WebhookPayload()
            {
                Action = "some-action"
            });

            //Assert
            Assert.IsFalse(canHandle);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_RepositoriesOfOtherInstallationIdAndCorrectInstallationIdPresent_DeletesCorrectInstallationIds()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogSettingsByGitHubInstallationIdQuery>(args =>
                    args.InstallationId == 1337))
                .Returns(new PullDogSettings()
                {
                    Repositories = new List<PullDogRepository>()
                    {
                        new PullDogRepository()
                        {
                            Handle = "correct-1",
                            GitHubInstallationId = 1337
                        },
                        new PullDogRepository()
                        {
                            Handle = "incorrect-1",
                            GitHubInstallationId = 1338
                        },
                        new PullDogRepository()
                        {
                            Handle = "correct-2",
                            GitHubInstallationId = 1337
                        },
                        new PullDogRepository()
                        {
                            Handle = "incorrect-1",
                            GitHubInstallationId = 1338
                        }
                    }
                });

            var handler = new UninstallationConfigurationPayloadHandler(
                fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayload()
            {
                Installation = new InstallationPayload()
                {
                    Id = 1337
                }
            });

            //Assert
            await fakeMediator
                .Received(2)
                .Send(Arg.Any<DeletePullDogRepositoryCommand>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeletePullDogRepositoryCommand>(args => 
                    args.Handle == "correct-1"));

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeletePullDogRepositoryCommand>(args =>
                    args.Handle == "correct-2"));
        }
    }
}
