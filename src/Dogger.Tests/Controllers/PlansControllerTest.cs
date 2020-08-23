using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using AutoMapper;
using Dogger.Controllers.Plans;
using Dogger.Domain.Commands.Instances.ProvisionDemoInstance;
using Dogger.Domain.Commands.Instances.ProvisionInstanceForUser;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Payment.GetActivePaymentMethodForUser;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Stripe;
using Plan = Dogger.Domain.Queries.Plans.GetSupportedPlans.Plan;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class PlansControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Get_OneSupportedPlanPresent_SupportedPlanReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetSupportedPlansQuery>())
                .Returns(new[]
                {
                    new Plan(
                        "some-bundle-id",
                        1337,
                        new Bundle(), 
                        Array.Empty<PullDogPlan>())
                });

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PlansController(
                mapper,
                fakeMediator);

            //Act
            var responses = await controller.Get();

            //Assert
            var responsesArray = responses.ToArray();
            Assert.AreEqual(1, responsesArray.Length);

            Assert.AreEqual("some-bundle-id", responsesArray.Single().Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetDemo_DemoPlanPresent_DemoPlanReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PlansController(
                mapper,
                fakeMediator);

            //Act
            var demoResponse = await controller.GetDemo();

            //Assert
            Assert.IsNotNull(demoResponse);

            Assert.AreEqual("some-bundle-id", demoResponse.Id);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionDemo_AuthenticatedUser_PassesAuthenticatedUserIdToProvisionDemoInstanceCommand()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            var fakeAuthenticatedUserId = Guid.NewGuid();
            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(
                    args => args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithId(fakeAuthenticatedUserId)
                    .Build());

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new PlansController(
                fakeMapper,
                fakeMediator);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ProvisionDemo();

            //Assert
            result.AssertSuccessfulStatusCode();

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ProvisionDemoInstanceCommand>(arg =>
                    arg.AuthenticatedUserId == fakeAuthenticatedUserId));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionDemo_DemoAlreadyProvisioned_ReturnsValidationError()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            fakeMediator
                .Send(Arg.Any<ProvisionDemoInstanceCommand>())
                .Throws(new DemoInstanceAlreadyProvisionedException());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PlansController(
                mapper,
                fakeMediator);

            //Act
            var result = await controller.ProvisionDemo();

            //Assert
            var validationProblem = result.GetValidationProblemDetails();

            Assert.AreEqual("ALREADY_PROVISIONED", validationProblem.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionDemo_DemoPlanPresentGiven_DemoInstanceProvisonedWithProperBundleId()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new PlansController(
                fakeMapper,
                fakeMediator);

            //Act
            await controller.ProvisionDemo();

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Any<ProvisionDemoInstanceCommand>());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionPlan_PaymentMethodAndNoPlanPresent_ReturnsValidationError()
        {
            //Arrange
            var signedInUserId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetActivePaymentMethodForUserQuery>(arg =>
                    arg.User.Id == signedInUserId))
                .Returns(new PaymentMethod());

            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(
                    args => args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithId(signedInUserId)
                    .Build());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PlansController(
                mapper,
                fakeMediator);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var response = await controller.ProvisionPlan("some-plan-id") as BadRequestObjectResult;

            //Assert
            Assert.IsNotNull(response);

            var value = response.Value as ValidationProblemDetails;
            Assert.IsNotNull(value);

            Assert.AreEqual("PLAN_NOT_FOUND", value.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionPlan_PlanAndNoPaymentMethodPresent_ReturnsValidationError()
        {
            //Arrange
            var signedInUserId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPlanByIdQuery>(arg =>
                    arg.Id == "some-plan-id"))
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(
                    args => args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithId(signedInUserId)
                    .Build());

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PlansController(
                mapper,
                fakeMediator);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var response = await controller.ProvisionPlan("some-plan-id") as BadRequestObjectResult;

            //Assert
            Assert.IsNotNull(response);

            var value = response.Value as ValidationProblemDetails;
            Assert.IsNotNull(value);

            Assert.AreEqual("NO_PAYMENT_METHOD", value.Type);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ProvisionPlan_PlanAndPaymentMethodPresent_InstanceProvisonedWithProperBundleId()
        {
            //Arrange
            var signedInUserId = Guid.NewGuid();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPlanByIdQuery>(arg =>
                    arg.Id == "some-plan-id"))
                .Returns(new Plan(
                    "some-bundle-id",
                    1337,
                    new Bundle(),
                    Array.Empty<PullDogPlan>()));

            fakeMediator
                .Send(Arg.Is<GetActivePaymentMethodForUserQuery>(arg =>
                    arg.User.Id == signedInUserId))
                .Returns(new PaymentMethod());

            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(
                    args => args.IdentityName == "some-identity-name"))
                .Returns(new TestUserBuilder()
                    .WithId(signedInUserId)
                    .Build());

            var fakeMapper = Substitute.For<IMapper>();

            var controller = new PlansController(
                fakeMapper,
                fakeMediator);
            controller.FakeAuthentication("some-identity-name");

            //Act
            await controller.ProvisionPlan("some-plan-id");

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ProvisionInstanceForUserCommand>(arg =>
                    arg.Plan.Id == "some-bundle-id" &&
                    arg.User.Id == signedInUserId));
        }
    }
}
