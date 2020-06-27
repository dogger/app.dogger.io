using System.Threading.Tasks;
using Dogger.Controllers.Payment;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Queries.Payment;
using Dogger.Infrastructure;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Stripe;

namespace Dogger.Tests.Controllers
{
    [TestClass]
    public class PaymentControllerTest
    {
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
