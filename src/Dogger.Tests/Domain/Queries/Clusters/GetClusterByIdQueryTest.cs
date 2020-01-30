using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Clusters.GetClusterById;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Clusters
{
    [TestClass]
    public class GetClusterByIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleClusters_ReturnsCorrectCluster()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    Id = guid1
                });

                await dataContext.Clusters.AddAsync(new Cluster()
                {
                    Id = guid2,
                    Instances = new List<Instance>()
                    {
                        new Instance()
                        {
                            Name = "non-demo",
                            PlanId = "dummy"
                        },
                        new Instance()
                        {
                            Name = "demo",
                            PlanId = "dummy"
                        }
                    }
                });
            });

            //Act
            var cluster = await environment.Mediator.Send(new GetClusterByIdQuery(guid2));

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(guid2, cluster.Id);
        }
    }
}
