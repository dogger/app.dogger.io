using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Commands.Users.CreateUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Ioc;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Commands.Payment
{
    [TestClass]
    public class SetActivePaymentMethodForUserCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_UserWithNoStripeCustomerId_ExceptionThrown()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<NoStripeCustomerIdException>(async () => 
                await environment.Mediator.Send(
                    new SetActivePaymentMethodForUserCommand(
                        new User(), 
                        "some-payment-method-id")));

            //Assert
            Assert.IsNotNull(exception);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingPaymentMethodsPresent_OldPaymentMethodsRemoved()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var paymentMethodService = environment
                .ServiceProvider
                .GetRequiredService<IOptionalService<PaymentMethodService>>();

            var existingPaymentMethod = await CreatePaymentMethodAsync(paymentMethodService.Value);
            await paymentMethodService.Value.AttachAsync(existingPaymentMethod.Id, new PaymentMethodAttachOptions()
            {
                Customer = user.StripeCustomerId
            });

            var newPaymentMethod = await CreatePaymentMethodAsync(paymentMethodService.Value);

            //Act
            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    newPaymentMethod.Id));

            //Assert
            var refreshedExistingPaymentMethod = await GetPaymentMethodForCustomerAsync(
                paymentMethodService.Value, 
                user.StripeCustomerId,
                existingPaymentMethod.Id);

            var refreshedNewPaymentMethod = await GetPaymentMethodForCustomerAsync(
                paymentMethodService.Value,
                user.StripeCustomerId,
                newPaymentMethod.Id);

            Assert.IsNotNull(refreshedNewPaymentMethod);
            Assert.IsNull(refreshedExistingPaymentMethod);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingPaymentMethodsPresent_DefaultPaymentMethodChangedToAddedMethod()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var paymentMethodService = environment
                .ServiceProvider
                .GetRequiredService<IOptionalService<PaymentMethodService>>();

            var existingPaymentMethod = await CreatePaymentMethodAsync(paymentMethodService.Value);
            await paymentMethodService.Value.AttachAsync(existingPaymentMethod.Id, new PaymentMethodAttachOptions()
            {
                Customer = user.StripeCustomerId
            });

            var newPaymentMethod = await CreatePaymentMethodAsync(paymentMethodService.Value);

            //Act
            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    newPaymentMethod.Id));

            //Assert
            var stripeCustomer = await environment
                .ServiceProvider
                .GetRequiredService<IOptionalService<CustomerService>>()
                .Value
                .GetAsync(user.StripeCustomerId);

            Assert.AreEqual(stripeCustomer.InvoiceSettings.DefaultPaymentMethodId, newPaymentMethod.Id);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoPaymentMethodsPresent_DefaultPaymentMethodChangedToAddedMethod()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = await environment.Mediator.Send(
                new CreateUserForIdentityCommand(
                    TestClaimsPrincipalFactory.CreateWithIdentityName("some-identity-name")));

            var paymentMethodService = environment
                .ServiceProvider
                .GetRequiredService<IOptionalService<PaymentMethodService>>();

            var newPaymentMethod = await CreatePaymentMethodAsync(paymentMethodService.Value);

            //Act
            await environment.Mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    newPaymentMethod.Id));

            //Assert
            var stripeCustomer = await environment
                .ServiceProvider
                .GetRequiredService<IOptionalService<CustomerService>>()
                .Value
                .GetAsync(user.StripeCustomerId);

            Assert.AreEqual(stripeCustomer.InvoiceSettings.DefaultPaymentMethodId, newPaymentMethod.Id);
        }

        private static async Task<PaymentMethod> GetPaymentMethodForCustomerAsync(
            PaymentMethodService paymentMethodService,
            string stripeCustomerId,
            string paymentMethodId)
        {
            var list = await paymentMethodService.ListAsync(new PaymentMethodListOptions()
            {
                Customer = stripeCustomerId,
                Type = "card"
            });
            return list.Data.SingleOrDefault(x => x.Id == paymentMethodId);
        }

        private static async Task<PaymentMethod> CreatePaymentMethodAsync(PaymentMethodService paymentMethodService)
        {
            return await paymentMethodService
                .CreateAsync(new PaymentMethodCreateOptions()
                {
                    Card = new PaymentMethodCardCreateOptions()
                    {
                        Number = "4242424242424242",
                        Cvc = "123",
                        ExpMonth = 10,
                        ExpYear = 30
                    },
                    Type = "card"
                });
        }
    }
}
