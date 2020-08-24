using System.Linq;
using System.Threading.Tasks;
using Dogger.Controllers.Deals;
using Dogger.Domain.Commands.Payment.AdjustUserBalance;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Queries.Payment.GetCouponByCode;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Stripe;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class DealsControllerTest
    {
        [TestMethod]
        public async Task ApplyAppSumo_CodeNotStartingWithProperPrefix_ReturnsBadRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var controller = new DealsController(fakeMediator);

            //Act
            var response = await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "dummy",
                Code = "some-invalid-code"
            });

            //Assert
            Assert.AreEqual(StatusCodes.Status400BadRequest, response.GetStatusCode());
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_ValidCode_InstallsPullDogFromGivenEmail()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 0, 0));

            var controller = new DealsController(fakeMediator);

            //Act
            await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.Single() == "some-email@example.com"));
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_NonExistingCodeSpecified_ReturnsNotFound()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 0, 0));

            var controller = new DealsController(fakeMediator);

            //Act
            var response = await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            Assert.AreEqual(StatusCodes.Status404NotFound, response.GetStatusCode());
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_CouponFoundButNotApplied_ReturnsBadRequest()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 0, 0));

            fakeMediator
                .Send(Arg.Is<GetCouponByCodeQuery>(args =>
                    args.Code == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(new PromotionCode());

            fakeMediator
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(args =>
                    args.CouponCode == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(false);

            var controller = new DealsController(fakeMediator);

            //Act
            var response = await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            Assert.AreEqual(StatusCodes.Status400BadRequest, response.GetStatusCode());
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_ValidConditions_ReturnsOk()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 0, 0));

            fakeMediator
                .Send(Arg.Is<GetCouponByCodeQuery>(args =>
                    args.Code == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(new PromotionCode()
                {
                    Coupon = new Coupon()
                    {
                        PercentOff = 0
                    }
                });

            fakeMediator
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(args =>
                    args.CouponCode == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(true);

            var user = new TestUserBuilder().Build();
            fakeMediator
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.Single() == "some-email@example.com"))
                .Returns(user);

            var controller = new DealsController(fakeMediator);

            //Act
            var response = await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            Assert.AreEqual(StatusCodes.Status200OK, response.GetStatusCode());
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_ValidConditions_AppliesCreditForCompensation()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 40_00, 0));

            fakeMediator
                .Send(Arg.Is<GetCouponByCodeQuery>(args =>
                    args.Code == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(new PromotionCode()
                {
                    Coupon = new Coupon()
                    {
                        PercentOff = 75
                    }
                });

            fakeMediator
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(args =>
                    args.CouponCode == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(true);

            var user = new TestUserBuilder().Build();
            fakeMediator
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.Single() == "some-email@example.com"))
                .Returns(user);

            var controller = new DealsController(fakeMediator);

            //Act
            await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<AdjustUserBalanceCommand>(args =>
                    args.IdempotencyId == "DEAL_APPSUMO_TOTALLY_VALID_CODE" &&
                    args.AdjustmentInHundreds == 120_00));
        }
        
        [TestMethod]
        public async Task ApplyAppSumo_ValidConditions_UpdatesSubscription()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>())
                .Returns(new PullDogPlan("dummy", 40_00, 0));

            fakeMediator
                .Send(Arg.Is<GetCouponByCodeQuery>(args =>
                    args.Code == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(new PromotionCode()
                {
                    Coupon = new Coupon()
                    {
                        PercentOff = 75
                    }
                });

            fakeMediator
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(args =>
                    args.CouponCode == "APPSUMO_TOTALLY_VALID_CODE"))
                .Returns(true);

            var user = new TestUserBuilder().Build();
            fakeMediator
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.Single() == "some-email@example.com"))
                .Returns(user);

            var controller = new DealsController(fakeMediator);

            //Act
            await controller.ApplyAppSumo(new ApplyAppSumoRequest()
            {
                Email = "some-email@example.com",
                Code = "APPSUMO_TOTALLY_VALID_CODE"
            });

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<UpdateUserSubscriptionCommand>(args =>
                    args.UserId == user.Id));
        }
    }
}
