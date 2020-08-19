using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromId;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.Plans.GetPullDogPlanFromId
{
    [TestClass]
    public class GetPullDogPlanFromIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SomeIntegrationCondition_SomeOutcome()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var result = await environment.Mediator.Send(
                new GetPullDogPlanFromIdQuery());

            //Assert
            Assert.Fail("Not implemented.");
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SomeUnitCondition_SomeOutcome()
        {
            //Arrange
            var handler = new GetPullDogPlanFromIdQueryHandler();

            //Act
            var result = await handler.Handle(
                new GetPullDogPlanFromIdQuery(),
                default);

            //Assert
            Assert.Fail("Not implemented.");
        }
    }
}

