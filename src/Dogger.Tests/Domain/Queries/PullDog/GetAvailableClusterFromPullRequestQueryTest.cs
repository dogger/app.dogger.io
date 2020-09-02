using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetAvailableClusterFromPullRequestQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ZeroPoolSizeAndExistingInstanceInCluster_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.PullDogDemoClusterId))
                .Returns(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPullDogPullRequest(new TestPullDogPullRequestBuilder()
                                .WithHandle("pr-1"))));

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<PullDogDemoInstanceAlreadyProvisionedException>(async () =>
                await handler.Handle(
                    new GetAvailableClusterFromPullRequestQuery(
                        new TestPullDogPullRequestBuilder()
                            .WithHandle("pr-2")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithPullDogSettings())),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ZeroPoolSizeAndNoExistingInstanceInCluster_ReturnsClusterWithAssignedUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureClusterWithIdCommand>(args => args.Id == DataContext.PullDogDemoClusterId))
                .Returns(new TestClusterBuilder().Build());

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            var user = new TestUserBuilder().Build();

            //Act
            var cluster = await handler.Handle(
                new GetAvailableClusterFromPullRequestQuery(
                    new TestPullDogPullRequestBuilder()
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                .WithUser(user)
                                .WithPoolSize(0)))),
                default);

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreSame(user, cluster.User);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NonZeroPoolSizeAndClusterFull_ThrowsException()
        {
            //Arrange
            var user = new TestUserBuilder().Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureClusterForUserCommand>(args =>
                    args.ClusterName == "pull-dog" &&
                    args.UserId == user.Id))
                .Returns(new TestClusterBuilder()
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithPullDogPullRequest(),
                        new TestInstanceBuilder()
                            .WithPullDogPullRequest()));

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<PullDogPoolSizeExceededException>(async () =>
                await handler.Handle(
                    new GetAvailableClusterFromPullRequestQuery(
                        new TestPullDogPullRequestBuilder()
                            .WithHandle("pr-2")
                            .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                    .WithUser(user)
                                    .WithPoolSize(2)))),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NonZeroPoolSizeAndNoExistingInstancesInCluster_ReturnsCluster()
        {
            //Arrange
            var user = new TestUserBuilder().Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<EnsureClusterForUserCommand>(args =>
                    args.ClusterName == "pull-dog" &&
                    args.UserId == user.Id))
                .Returns(new TestClusterBuilder().Build());

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var cluster = await handler.Handle(
                new GetAvailableClusterFromPullRequestQuery(
                    new TestPullDogPullRequestBuilder()
                        .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                            .WithPullDogSettings(new TestPullDogSettingsBuilder()
                                .WithUser(user)
                                .WithPoolSize(1)))),
                default);

            //Assert
            Assert.IsNotNull(cluster);
        }
    }
}
