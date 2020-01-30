using System;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.Plans
{
    [TestClass]
    public class GetDemoPlanQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_PlansAboveAndBelowFourGigabytesOfRam_FirstMinimumFourGigabyteRamPlanSelected()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetSupportedPlansQuery>(),
                    default)
                .Returns(new[]
                {
                    new Plan(
                        "some-bundle-1",
                        1337,
                        new Bundle()
                        {
                            RamSizeInGb = 2,
                            BundleId = "some-bundle-1"
                        },
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "some-bundle-2",
                        1337,
                        new Bundle()
                        {
                            RamSizeInGb = 4,
                            BundleId = "some-bundle-2"
                        },
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "some-bundle-3",
                        1337,
                        new Bundle()
                        {
                            RamSizeInGb = 1,
                            BundleId = "some-bundle-3"
                        },
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "some-bundle-4",
                        1337,
                        new Bundle()
                        {
                            RamSizeInGb = 6,
                            BundleId = "some-bundle-4"
                        },
                        Array.Empty<PullDogPlan>())
                });

            var handler = new GetDemoPlanQueryHandler(
                fakeMediator);

            //Act
            var demoPlan = await handler.Handle(
                new GetDemoPlanQuery(),
                default);

            //Assert
            Assert.IsNotNull(demoPlan);
            Assert.AreEqual("some-bundle-2", demoPlan.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_AllPlansSameRamAboveFourGigabytes_PicksCheapestPlan()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(
                    Arg.Any<GetSupportedPlansQuery>(),
                    default)
                .Returns(new[]
                {
                    new Plan(
                        "some-bundle-1",
                        3,
                        new Bundle()
                        {
                            RamSizeInGb = 4,
                            BundleId = "some-bundle-1"
                        },
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "some-bundle-2",
                        1,
                        new Bundle()
                        {
                            RamSizeInGb = 4,
                            BundleId = "some-bundle-2"
                        },
                        Array.Empty<PullDogPlan>()),
                    new Plan(
                        "some-bundle-3",
                        5,
                        new Bundle()
                        {
                            RamSizeInGb = 4,
                            BundleId = "some-bundle-3"
                        },
                        Array.Empty<PullDogPlan>())
                });

            var handler = new GetDemoPlanQueryHandler(
                fakeMediator);

            //Act
            var demoPlan = await handler.Handle(
                new GetDemoPlanQuery(),
                default);

            //Assert
            Assert.IsNotNull(demoPlan);
            Assert.AreEqual("some-bundle-2", demoPlan.Id);
        }
    }
}
