using System;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Plans
{
    [TestClass]
    public class GetPullDogPlanFromSettingsTest
    {
        [TestMethod]
        public async Task Handle_DoggerPlanNotFound_ReturnsNull()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPlanByIdQuery>(args =>
                    args.Id == "non-existing"))
                .Returns((Plan)null);

            var handler = new GetPullDogPlanFromSettingsQueryHandler(fakeMediator);

            //Act
            var plan = await handler.Handle(
                new GetPullDogPlanFromSettingsQuery(
                    "non-existing",
                    1337),
                default);

            //Assert
            Assert.IsNull(plan);
        }

        [TestMethod]
        public async Task Handle_DoggerPlanFoundButMatchingPullDogPlanNotFound_ReturnsNull()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPlanByIdQuery>(args =>
                    args.Id == "some-plan-id"))
                .Returns(new Plan(
                    "some-plan-id",
                    1337,
                    new Bundle(), 
                    Array.Empty<PullDogPlan>()));

            var handler = new GetPullDogPlanFromSettingsQueryHandler(fakeMediator);

            //Act
            var plan = await handler.Handle(
                new GetPullDogPlanFromSettingsQuery(
                    "some-plan-id",
                    1337),
                default);

            //Assert
            Assert.IsNull(plan);
        }

        [TestMethod]
        public async Task Handle_PullDogPlanFound_ReturnsMatchingPlan()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPlanByIdQuery>(args =>
                    args.Id == "some-plan-id"))
                .Returns(new Plan(
                    "some-plan-id",
                    1337,
                    new Bundle(),
                    new []
                    {
                        new PullDogPlan("dummy", 1337, 1337)
                    }));

            var handler = new GetPullDogPlanFromSettingsQueryHandler(fakeMediator);

            //Act
            var plan = await handler.Handle(
                new GetPullDogPlanFromSettingsQuery(
                    "some-plan-id",
                    1337),
                default);

            //Assert
            Assert.IsNotNull(plan);
        }
    }
}
