using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Plans
{
    [TestClass]
    public class GetPlanByIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_MultiplePlansPresent_ReturnsMatchingPlan()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetSupportedPlansQuery>())
                .Returns(new [] {
                    new Plan(
                        "non-matching-1",
                        1337,
                        new Bundle(), 
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "matching",
                        1337,
                        new Bundle(),
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "non-matching-2",
                        1337,
                        new Bundle(), 
                        Array.Empty<PullDogPlan>())
                });

            var handler = new GetPlanByIdQueryHandler(
                fakeMediator);

            //Act
            var bundle = await handler.Handle(new GetPlanByIdQuery("matching"), default);

            //Assert
            Assert.IsNotNull(bundle);
            Assert.AreEqual("matching", bundle.Id);
        }
    }
}
