using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.PullDog.Webhooks
{
    [TestClass]
    public class PullRequestReadyPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionOpenedAndPullRequestStateOpen_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "opened",
                PullRequest = new PullRequestPayload()
                {
                    State = "open"
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionReopenedAndPullRequestStateOpen_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "reopened",
                PullRequest = new PullRequestPayload()
                {
                    State = "open"
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionSynchronizeAndPullRequestStateOpen_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "synchronize",
                PullRequest = new PullRequestPayload()
                {
                    State = "open"
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionReadyForReviewAndPullRequestStateOpen_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "ready_for_review",
                PullRequest = new PullRequestPayload()
                {
                    State = "open"
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionReadyForReviewAndPullRequestInDraftMode_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "ready_for_review",
                PullRequest = new PullRequestPayload()
                {
                    State = "open",
                    Draft = true
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionInvalidAndPullRequestStateOpen_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "invalid",
                PullRequest = new PullRequestPayload()
                {
                    State = "open"
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionOpenedAndPullRequestStateInvalid_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "opened",
                PullRequest = new PullRequestPayload()
                {
                    State = "invalid"
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestOpenedPayloadWithValidRepository_ProvisionsPullDogInstance()
        {
            //Arrange
            var databaseRepository = new TestPullDogRepositoryBuilder()
                .WithHandle("1337")
                .Build();

            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestReadyPayloadHandler(fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayloadContext(
                new WebhookPayload(),
                null!,
                databaseRepository,
                new TestPullDogPullRequestBuilder()
                    .WithHandle("1338")));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ProvisionPullDogEnvironmentCommand>(args =>
                    args.PullRequestHandle == "1338" &&
                    args.Repository == databaseRepository));
        }
    }
}
