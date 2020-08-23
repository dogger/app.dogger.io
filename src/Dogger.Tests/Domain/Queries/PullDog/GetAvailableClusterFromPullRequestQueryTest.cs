using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
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
                .Returns(new Cluster()
                {
                    Instances = new List<Instance>()
                    {
                        new Instance()
                        {
                            PullDogPullRequest = new PullDogPullRequest()
                            {
                                Handle = "pr-1"
                            }
                        }
                    }
                });

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<PullDogDemoInstanceAlreadyProvisionedException>(async () =>
                await handler.Handle(
                    new GetAvailableClusterFromPullRequestQuery(
                        new PullDogPullRequest()
                        {
                            Handle = "pr-2",
                            PullDogRepository = new PullDogRepository()
                            {
                                PullDogSettings = new PullDogSettings()
                                {
                                    User = new TestUserBuilder().Build(),
                                    PoolSize = 0
                                }
                            }
                        }),
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
                .Returns(new Cluster());

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            var user = new TestUserBuilder().Build();

            //Act
            var cluster = await handler.Handle(
                new GetAvailableClusterFromPullRequestQuery(
                    new PullDogPullRequest()
                    {
                        Handle = "dummy",
                        PullDogRepository = new PullDogRepository()
                        {
                            PullDogSettings = new PullDogSettings()
                            {
                                User = user,
                                PoolSize = 0
                            }
                        }
                    }),
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
                .Returns(new Cluster()
                {
                    Instances = new List<Instance>()
                    {
                        new Instance()
                        {
                            PullDogPullRequest = new PullDogPullRequest()
                            {
                                Handle = "dummy-1"
                            }
                        },
                        new Instance()
                        {
                            PullDogPullRequest = new PullDogPullRequest()
                            {
                                Handle = "dummy-2"
                            }
                        }
                    }
                });

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<PullDogPoolSizeExceededException>(async () =>
                await handler.Handle(
                    new GetAvailableClusterFromPullRequestQuery(
                        new PullDogPullRequest()
                        {
                            Handle = "pr-2",
                            PullDogRepository = new PullDogRepository()
                            {
                                PullDogSettings = new PullDogSettings()
                                {
                                    User = user,
                                    PoolSize = 2
                                }
                            }
                        }),
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
                .Returns(new Cluster()
                {
                    Instances = new List<Instance>()
                });

            var handler = new GetAvailableClusterFromPullRequestQueryHandler(
                fakeMediator,
                Substitute.For<IPullDogRepositoryClientFactory>());

            //Act
            var cluster = await handler.Handle(
                new GetAvailableClusterFromPullRequestQuery(
                    new PullDogPullRequest()
                    {
                        PullDogRepository = new PullDogRepository()
                        {
                            PullDogSettings = new PullDogSettings()
                            {
                                User = user,
                                PoolSize = 1
                            }
                        }
                    }),
                default);

            //Assert
            Assert.IsNotNull(cluster);
        }
    }
}
