using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.Clusters
{
    [TestClass]
    public class EnsureClusterWithIdCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleClusters_ReturnsDemoCluster()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Clusters.AddAsync(new TestClusterBuilder().Build());

                await dataContext.Clusters.AddAsync(new TestClusterBuilder()
                    .WithId(DataContext.DemoClusterId));
            });

            //Act
            var cluster = await environment.Mediator.Send(new EnsureClusterWithIdCommand(DataContext.DemoClusterId));

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(DataContext.DemoClusterId, cluster.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoClusters_CreatesDemoCluster()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var cluster = await environment.Mediator.Send(new EnsureClusterWithIdCommand(DataContext.DemoClusterId));

            //Assert
            Assert.IsNotNull(cluster);
            Assert.AreEqual(DataContext.DemoClusterId, cluster.Id);
        }
    }
}
