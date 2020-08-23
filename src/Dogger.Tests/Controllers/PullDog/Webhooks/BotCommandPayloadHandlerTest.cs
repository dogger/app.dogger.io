using System;
using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.PullDog.Webhooks
{
    [TestClass]
    public class BotCommandPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionNotCreated_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "invalid",
                Issue = new IssuePayload()
                {
                    PullRequest = new IssuePullRequestPayload()
                },
                Comment = new CommentPayload()
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ValidConditions_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "created",
                Issue = new IssuePayload()
                {
                    PullRequest = new IssuePullRequestPayload()
                },
                Comment = new CommentPayload()
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NullTextPresent_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.HandleAsync(new WebhookPayloadContext(
                    new WebhookPayload()
                    {
                        Comment = new CommentPayload()
                        {
                            Body = null
                        }
                    },
                    null!,
                    null!,
                    null!)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GoFetchCommandPresentWithEnvironmentName_ProvisionsPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var fakeHostEnvironment = Substitute.For<IHostEnvironment>();
            fakeHostEnvironment.EnvironmentName.Returns("environment");

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                fakeHostEnvironment);

            //Act
            await handler.HandleAsync(new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Comment = new CommentPayload()
                    {
                        Body = "@pull-dog up environment"
                    }
                },
                null!,
                new PullDogRepository(),
                new TestPullDogPullRequestBuilder().Build()));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<ProvisionPullDogEnvironmentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GoFetchCommandPresent_ProvisionsPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            await handler.HandleAsync(new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Comment = new CommentPayload()
                    {
                        Body = "@pull-dog up"
                    }
                },
                null!,
                new PullDogRepository(),
                new TestPullDogPullRequestBuilder().Build()));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<ProvisionPullDogEnvironmentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_GetLostCommandPresent_DeletesPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(
                fakeMediator,
                Substitute.For<IHostEnvironment>());

            //Act
            await handler.HandleAsync(new WebhookPayloadContext(
                new WebhookPayload()
                {
                    Comment = new CommentPayload()
                    {
                        Body = "@pull-dog down"
                    }
                },
                null!,
                new PullDogRepository(),
                new TestPullDogPullRequestBuilder().Build()));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<DeleteInstanceByPullRequestCommand>());
        }
    }
}
