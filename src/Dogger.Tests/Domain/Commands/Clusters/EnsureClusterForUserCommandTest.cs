using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var matchingClusterId = Guid.NewGuid();
            var user = new User()
            {
                StripeCustomerId = "dummy",
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Name = "some-non-matching-cluster-1"
                    },
                    new Cluster()
                    {
                        Id = matchingClusterId,
                        Name = "some-matching-cluster"
                    },
                    new Cluster()
                    {
                        Name = "some-non-matching-cluster-2"
                    }
                }
            };
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var user = new User()
            {
                StripeCustomerId = "dummy",
                Clusters = new List<Cluster>()
                {
                    new Cluster()
                    {
                        Name = "some-non-matching-cluster-1"
                    },
                    new Cluster()
                    {
                        Name = "some-non-matching-cluster-2"
                    }
                }
            };
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
