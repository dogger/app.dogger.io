﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.PullDog.Webhooks
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
                .Returns(new TestPullDogSettingsBuilder()
                    .WithRepositories(
                        new TestPullDogRepositoryBuilder()
                            .WithHandle("correct-1")
                            .WithGitHubInstallationId(1337)
                            .Build(),
                        new TestPullDogRepositoryBuilder()
                            .WithHandle("incorrect-1")
                            .WithGitHubInstallationId(1338)
                            .Build(),
                        new TestPullDogRepositoryBuilder()
                            .WithHandle("correct-2")
                            .WithGitHubInstallationId(1337)
                            .Build(),
                        new TestPullDogRepositoryBuilder()
                            .WithHandle("incorrect-2")
                            .WithGitHubInstallationId(1338)));

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
