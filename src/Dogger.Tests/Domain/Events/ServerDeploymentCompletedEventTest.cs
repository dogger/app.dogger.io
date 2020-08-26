using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.ServerDeploymentCompleted;
using Dogger.Domain.Queries.Clusters.GetConnectionDetails;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Events
{
    [TestClass]
    public class ServerDeploymentCompletedEventTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoInstanceFoundByName_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var handler = new ServerDeploymentCompletedEventHandler(fakeMediator);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new ServerDeploymentCompletedEvent("some-instance-name"),
                    default));

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoPullRequestOnInstance_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args => args.Name == "some-instance-name"))
                .Returns(new TestInstanceBuilder()
                    .WithPullDogPullRequest(null));

            var handler = new ServerDeploymentCompletedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerDeploymentCompletedEvent("some-instance-name"),
                default);

            //Assert
            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoConnectionDetailsFound_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args => args.Name == "some-instance-name"))
                .Returns(new TestInstanceBuilder()
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder().Build()));

            var handler = new ServerDeploymentCompletedEventHandler(fakeMediator);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new ServerDeploymentCompletedEvent("some-instance-name"),
                    default));

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .DidNotReceive()
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidConditions_UpsertsCommentInPullRequestWithConnectionDetails()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetInstanceByNameQuery>(args => args.Name == "some-instance-name"))
                .Returns(new TestInstanceBuilder()
                    .WithPullDogPullRequest(new TestPullDogPullRequestBuilder().Build()));

            fakeMediator
                .Send(Arg.Is<GetConnectionDetailsQuery>(args => args.ClusterId == "some-instance-name"))
                .Returns(new ConnectionDetailsResponse(
                    "some-ip-address",
                    "some-host-name",
                    new[]
                    {
                        new ExposedPort()
                        {
                            Port = 1337,
                            Protocol = SocketProtocol.Udp
                        }
                    }));

            var handler = new ServerDeploymentCompletedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerDeploymentCompletedEvent("some-instance-name"),
                default);

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<UpsertPullRequestCommentCommand>(args =>
                    args.Content.Contains("some-host-name") &&
                    args.Content.Contains("1337") &&
                    args.Content.Contains("UDP")));
        }
    }
}
