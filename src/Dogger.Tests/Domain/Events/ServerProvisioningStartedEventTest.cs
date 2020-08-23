using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.AddLabelToGitHubPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Events.ServerProvisioningStarted;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.Domain.Models;
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
                    new TestInstanceBuilder()
                        .WithPullDogPullRequest(null)
                        .Build()),
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
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile());

            var handler = new ServerProvisioningStartedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerProvisioningStartedEvent(
                    new TestInstanceBuilder()
                        .WithPullDogPullRequest(new PullDogPullRequest())
                        .Build()),
                default);

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<UpsertPullRequestCommentCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PullRequestOnInstanceWithLabelOnConfiguration_AddsLabelToPullRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<MediatR.IMediator>();
            fakeMediator
                .Send(Arg.Any<GetConfigurationForPullRequestQuery>())
                .Returns(new ConfigurationFile()
                {
                    Label = "some-label"
                });

            var handler = new ServerProvisioningStartedEventHandler(fakeMediator);

            //Act
            await handler.Handle(
                new ServerProvisioningStartedEvent(
                    new TestInstanceBuilder()
                        .WithPullDogPullRequest(new PullDogPullRequest())
                        .Build()),
                default);

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<AddLabelToGitHubPullRequestCommand>(args =>
                    args.Label == "some-label"));
        }
    }
}
