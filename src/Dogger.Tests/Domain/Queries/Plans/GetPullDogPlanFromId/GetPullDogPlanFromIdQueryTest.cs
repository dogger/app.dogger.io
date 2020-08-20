using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromId;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Plans.GetPullDogPlanFromId
{
    [TestClass]
    public class GetPullDogPlanFromIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_ValidPlanSpecified_ReturnsMatchingPlan()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetSupportedPullDogPlansQuery>())
                .Returns(new[]
                {
                    new PullDogPlan("non-matching", 0, 0), 
                    new PullDogPlan("some-id", 0, 0)
                });

            var handler = new GetPullDogPlanFromIdQueryHandler(fakeMediator);

            //Act
            var result = await handler.Handle(
                new GetPullDogPlanFromIdQuery("some-id"),
                default);

            //Assert
            Assert.AreEqual("some-id", result.Id);
        }
    }
}

