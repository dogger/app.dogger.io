using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Clusters.GetClusterForUser;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Clusters
{
    [TestClass]
    public class GetClusterForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoClusterIdGivenAndSingleClusterOnUser_ClusterReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var fakeUserId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    User = new TestUserBuilder()
                        .WithId(fakeUserId)
                        .Build()
                });
            });

            //Act
            var cluster = await environment.Mediator.Send(new GetClusterForUserQuery(fakeUserId));

            //Assert
            Assert.IsNotNull(cluster);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ClusterIdGivenAndOneMatchingClusterOnUser_ClusterReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var fakeUserId = Guid.NewGuid();
            var fakeClusterId = Guid.NewGuid();

            var user = new TestUserBuilder()
                .WithId(fakeUserId)
                .Build();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    User = user
                });
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    Id = fakeClusterId,
                    User = user
                });
            });

            //Act
            var cluster = await environment.Mediator.Send(new GetClusterForUserQuery(fakeUserId)
            {
                ClusterId = fakeClusterId
            });

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(fakeClusterId, cluster.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoClusterIdGivenAndMultipleClustersOnUser_QueryTooBroadExceptionThrown()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var fakeUserId = Guid.NewGuid();

            var user = new TestUserBuilder()
                .WithId(fakeUserId)
                .Build();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    User = user
                });
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    User = user
                });
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<ClusterQueryTooBroadException>(async () =>
                await environment.Mediator.Send(new GetClusterForUserQuery(fakeUserId)));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}
