﻿using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Instances
{
    [TestClass]
    public class GetInstanceByNameQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoInstancesPresent_NullIsReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var result = await environment.Mediator.Send(new GetInstanceByNameQuery("some-instance-name"));

            //Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleInstancesPresent_MatchingInstanceIsReturned()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Instances.AddAsync(new Instance()
                {
                    Name = "non-matching-name",
                    PlanId = "dummy",
                    Cluster = new TestClusterBuilder().Build()
                });
                await dataContext.Instances.AddAsync(new Instance()
                {
                    Name = "some-instance-name",
                    PlanId = "dummy",
                    Cluster = new TestClusterBuilder().Build()
                });
            });

            //Act
            var result = await environment.Mediator.Send(new GetInstanceByNameQuery("some-instance-name"));

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("some-instance-name", result.Name);
        }
    }
}
