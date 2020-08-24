using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.Clusters
{
    [TestClass]
    public class EnsureClusterForUserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleClustersOnUserWithDifferentNames_ReturnsMatchingClusterByName()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var matchingClusterId = Guid.NewGuid();
            var user = new TestUserBuilder()
                .WithClusters(
                    new TestClusterBuilder()
                        .WithName("some-non-matching-cluster-1")
                        .Build(),
                    new TestClusterBuilder()
                        .WithName("some-matching-cluster")
                        .Build(),
                    new TestClusterBuilder()
                        .WithName("some-non-matching-cluster-2"))
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var cluster = await environment.Mediator.Send(new EnsureClusterForUserCommand(user.Id)
            {
                ClusterName = "some-matching-cluster"
            });

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(cluster.UserId, user.Id);
            Assert.AreEqual("some-matching-cluster", cluster.Name);
            Assert.AreEqual(matchingClusterId, cluster.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoClustersOnUserByName_CreatesClusterByName()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = new TestUserBuilder()
                .WithClusters(
                    new TestClusterBuilder()
                        .WithName("some-non-matching-cluster-1")
                        .Build(),
                    new TestClusterBuilder()
                        .WithName("some-non-matching-cluster-2"))
                .Build();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var cluster = await environment.Mediator.Send(new EnsureClusterForUserCommand(user.Id)
            {
                ClusterName = "some-cluster-name"
            });

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(cluster.UserId, user.Id);
            Assert.AreEqual("some-cluster-name", cluster.Name);
        }
    }
}
