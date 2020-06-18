using System.Threading.Tasks;
using Dogger.Controllers.Webhooks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Controllers.Webhooks
{
    [TestClass]
    public class PullRequestClosedPayloadHandlerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_ActionNotClosed_ReturnsFalse()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestClosedPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "opened",
                PullRequest = new PullRequestPayload()
                {
                    State = "closed"
                }
            });

            //Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task CanHandle_PullRequestClosedAndActionClosed_ReturnsTrue()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestClosedPayloadHandler(fakeMediator);

            //Act
            var result = handler.CanHandle(new WebhookPayload()
            {
                Action = "closed",
                PullRequest = new PullRequestPayload()
                {
                    State = "closed"
                }
            });

            //Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestClosedPayload_DeletesPullDogInstance()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new PullRequestClosedPayloadHandler(fakeMediator);

            //Act
            await handler.HandleAsync(new WebhookPayloadContext(
                null!,
                null!,
                new PullDogRepository()
                {
                    Handle = "1337"
                },
                new PullDogPullRequest()
                {
                    Handle = "1338"
                }));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByPullRequestCommand>(args =>
                    args.RepositoryHandle == "1337" &&
                    args.PullRequestHandle == "1338"));
        }
    }
}
