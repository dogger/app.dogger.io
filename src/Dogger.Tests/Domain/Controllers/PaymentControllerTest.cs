using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Controllers.Payment;
using Dogger.Domain.Queries.Payment.GetActivePaymentMethodForUser;
using Dogger.Domain.Queries.Payment.GetCouponForUser;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Stripe;

namespace Dogger.Tests.Domain.Controllers
{
    [TestClass]
    public class PaymentControllerTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetCoupon_CouponExists_CouponReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetCouponForUserQuery>())
                .Returns(new PromotionCode()
                {
                    Code = "some-promotion-code"
                });

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var response = await controller.GetCoupon();

            //Assert
            var coupon = response.ToObject<CouponCodeResponse>();
            Assert.IsNotNull(coupon);
            Assert.AreEqual("some-promotion-code", coupon.Code);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetCoupon_CouponDoesNotExist_NullReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetCouponForUserQuery>())
                .Returns((PromotionCode)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var response = await controller.GetCoupon();

            //Assert
            Assert.AreEqual(StatusCodes.Status204NoContent, response.GetStatusCode());
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task ApplyCoupon_UserSignedIn_AppliesCoupon()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(args =>
                    args.CouponCode == "some-coupon-code"))
                .Returns(true);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var result = await controller.ApplyCoupon("some-coupon-code");

            //Assert
            var response = result.ToObject<ApplyCouponResponse>();
            Assert.IsTrue(response.WasApplied);

            await fakeMediator
                .Received(1)
                .Send(Arg.Is<ApplyCouponCodeForUserCommand>(arg =>
                    arg.CouponCode == "some-coupon-code"));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetCurrentPaymentMethod_ActivePaymentMethodExists_PaymentMethodReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetActivePaymentMethodForUserQuery>())
                .Returns(new PaymentMethod()
                {
                    Id = "some-payment-method-id"
                });

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var paymentMethodResponse = await controller.GetCurrentPaymentMethod();

            //Assert
            var paymentMethod = paymentMethodResponse.ToObject<PaymentMethodResponse>();
            Assert.IsNotNull(paymentMethod);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetCurrentPaymentMethod_ActivePaymentMethodDoesNotExist_NullReturned()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetActivePaymentMethodForUserQuery>())
                .Returns((PaymentMethod)null);

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            var paymentMethodResponse = await controller.GetCurrentPaymentMethod();

            //Assert
            var paymentMethod = paymentMethodResponse.ToObject<PaymentMethodResponse>();
            Assert.IsNull(paymentMethod);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task AddPaymentMethod_UserSignedIn_SetsActivePaymentMethodForUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            var mapper = AutoMapperFactory.CreateValidMapper();

            var controller = new PaymentController(
                fakeMediator,
                mapper);
            controller.FakeAuthentication("some-identity-name");

            //Act
            await controller.AddPaymentMethod("some-payment-method-id");

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<SetActivePaymentMethodForUserCommand>(arg =>
                    arg.PaymentMethodId == "some-payment-method-id"));
        }
    }
}
