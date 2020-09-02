using System;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Clusters.GetClusterById;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
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
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var guid1 = Guid.NewGuid();
            var guid2 = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new TestClusterBuilder()
                    .WithId(guid1));

                await dataContext.Clusters.AddAsync(new TestClusterBuilder()
                    .WithId(guid2)
                    .WithInstances(
                        new TestInstanceBuilder()
                            .WithName("non-demo"),
                        new TestInstanceBuilder()
                            .WithName("demo")));
            });

            //Act
            var cluster = await environment.Mediator.Send(new GetClusterByIdQuery(guid2));

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(guid2, cluster.Id);
        }
    }
}
