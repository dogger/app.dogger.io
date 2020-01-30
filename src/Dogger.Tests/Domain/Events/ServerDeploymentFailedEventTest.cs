using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.ServerDeploymentFailed;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Events
{
    [TestClass]
    public class ServerDeploymentFailedEventTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstanceNotFound_DeletesInstanceByNameWithoutInsertingPullDogComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ServerDeploymentFailedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerDeploymentFailedEvent(
                    "some-instance-name",
                    "dummy"),
                default);

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstanceFoundWithNoPullDogInformation_DeletesInstanceByNameWithoutInsertingPullDogComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args =>
                    args.Name == "some-instance-name"))
                .Returns(new Instance());

            var handler = new ServerDeploymentFailedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerDeploymentFailedEvent(
                    "some-instance-name",
                    "dummy"),
                default);

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_InstanceFoundWithPullDogInformation_DeletesInstanceByNameAndInsertsPullDogComment()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args =>
                    args.Name == "some-instance-name"))
                .Returns(new Instance()
                {
                    PullDogPullRequest = new PullDogPullRequest()
                });

            var handler = new ServerDeploymentFailedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerDeploymentFailedEvent(
                    "some-instance-name",
                    "dummy"),
                default);

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteInstanceByNameCommand>(args =>
                    args.Name == "some-instance-name"));
        }
    }
}
