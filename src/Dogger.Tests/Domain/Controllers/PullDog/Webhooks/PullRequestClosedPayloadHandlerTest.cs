using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Domain.Controllers.PullDog.Webhooks.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Controllers.PullDog.Webhooks
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
                new TestPullDogRepositoryBuilder()
                    .WithHandle("1337"),
                new TestPullDogPullRequestBuilder()
                    .WithHandle("1338")));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByPullRequestCommand>(args =>
                    args.RepositoryHandle == "1337" &&
                    args.PullRequestHandle == "1338"));
        }
    }
}
