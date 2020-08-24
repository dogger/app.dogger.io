using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Models.Builders;
using Dogger.Domain.Queries.Instances.GetExpiredInstances;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Instances
{
    [TestClass]
    public class GetExpiredInstancesQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesPresentWithNoExpiry_ReturnsEmptyList()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()));

            //Act
            var expiredInstances = await environment.Mediator.Send(new GetExpiredInstancesQuery());

            //Assert
            Assert.IsNotNull(expiredInstances);
            Assert.AreEqual(0, expiredInstances.Length);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesPresentFutureExpiry_ReturnsEmptyList()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithExpiredDate(DateTime.UtcNow.AddMinutes(1))));

            //Act
            var expiredInstances = await environment.Mediator.Send(new GetExpiredInstancesQuery());

            //Assert
            Assert.IsNotNull(expiredInstances);
            Assert.AreEqual(0, expiredInstances.Length);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InstancesPresentWithMixedExpiry_ReturnsExpiredInstances()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithName("non-expiring-1")
                    .WithExpiredDate(DateTime.UtcNow.AddMinutes(1)));

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithName("expiring-1")
                    .WithExpiredDate(DateTime.UtcNow.AddMinutes(-1)));

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithName("non-expiring-2")
                    .WithExpiredDate(DateTime.UtcNow.AddMinutes(3)));

                await dataContext.Instances.AddAsync(new TestInstanceBuilder()
                    .WithCluster()
                    .WithName("expiring-2")
                    .WithExpiredDate(DateTime.UtcNow.AddMinutes(-3)));
            });

            //Act
            var expiredInstances = await environment.Mediator.Send(new GetExpiredInstancesQuery());

            //Assert
            Assert.IsNotNull(expiredInstances);
            Assert.AreEqual(2, expiredInstances.Length);

            Assert.IsTrue(expiredInstances.Any(x => x.Name == "expiring-1"));
            Assert.IsTrue(expiredInstances.Any(x => x.Name == "expiring-2"));
        }
    }
}
