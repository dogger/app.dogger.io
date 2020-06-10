using System;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.Webhooks
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

            var handler = new BotCommandPayloadHandler(fakeMediator);

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
        public async Task CanHandle_PullRequestNotProvided_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "created",
                Issue = new IssuePayload()
                {
                    PullRequest = null
                },
                Comment = new CommentPayload()
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_IssueNotProvided_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "created",
                Issue = null,
                Comment = new CommentPayload()
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_CommentNotProvided_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "created",
                Issue = new IssuePayload()
                {
                    PullRequest = new IssuePullRequestPayload()
                },
                Comment = null
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

            var handler = new BotCommandPayloadHandler(fakeMediator);

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

            var handler = new BotCommandPayloadHandler(fakeMediator);

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
        public async Task Handle_GoFetchCommandPresent_ProvisionsPullDogEnvironment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new BotCommandPayloadHandler(fakeMediator);

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
                new PullDogPullRequest()));

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

            var handler = new BotCommandPayloadHandler(fakeMediator);

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
                new PullDogPullRequest()));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<DeleteInstanceByPullRequestCommand>());
        }
    }
}
