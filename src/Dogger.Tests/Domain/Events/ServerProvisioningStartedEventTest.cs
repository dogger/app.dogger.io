using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.ServerProvisioningStarted;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Events
{
    [TestClass]
    public class ServerProvisioningStartedEventTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoPullRequestOnGivenInstance_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<MediatR.IMediator>();

            var handler = new ServerProvisioningStartedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerProvisioningStartedEvent(
                    new Instance()
                    {
                        PullDogPullRequest = null
                    }),
                default);

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestOnInstance_UpsertsPullRequestComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<MediatR.IMediator>();

            var handler = new ServerProvisioningStartedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerProvisioningStartedEvent(
                    new Instance()
                    {
                        PullDogPullRequest = new PullDogPullRequest()
                    }),
                default);

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }
    }
}
