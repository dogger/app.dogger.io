using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetSubscriptionById;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stripe;

namespace Dogger.Tests.Domain.Queries.Payment.GetSubscriptionById
{
    [TestClass]
    public class GetSubscriptionByIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SubscriptionNotFound_ReturnsNull()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var result = await environment.Mediator.Send(
                new GetSubscriptionByIdQuery("non-existing-subscription"));

            //Assert
            Assert.IsNull(result);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ActiveSubscriptionFound_ReturnsSubscription()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var planService = environment.ServiceProvider.GetRequiredService<PlanService>();
            var plan = await planService.CreateAsync(new PlanCreateOptions()
            {
                Interval = "month",
                Currency = "usd",
                Amount = 1_00,
                Product = new PlanProductCreateOptions()
                {
                    Name = "dummy"
                }
            });

            var customerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions());

            var paymentMethodService = environment.ServiceProvider.GetRequiredService<PaymentMethodService>();
            var paymentMethod = await paymentMethodService.CreateAsync(new PaymentMethodCreateOptions()
            {
                Type = "card",
                Card = new PaymentMethodCardCreateOptions()
                {
                    Cvc = "123",
                    ExpMonth = 11,
                    ExpYear = 2030,
                    Number = "4242424242424242",
                    Token = null
                }
            });

            await paymentMethodService.AttachAsync(paymentMethod.Id, new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var subscriptionService = environment.ServiceProvider.GetRequiredService<SubscriptionService>();
            var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = customer.Id,
                DefaultPaymentMethod = paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = plan.Id
                    }
                }
            });

            //Act
            var result = await environment.Mediator.Send(
                new GetSubscriptionByIdQuery(subscription.Id));

            //Assert
            Assert.IsNotNull(result);

            Assert.AreEqual(subscription.Id, result.Id);
        }
        
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_CanceledSubscriptionFound_ReturnsNull()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var planService = environment.ServiceProvider.GetRequiredService<PlanService>();
            var plan = await planService.CreateAsync(new PlanCreateOptions()
            {
                Interval = "month",
                Currency = "usd",
                Amount = 1_00,
                Product = new PlanProductCreateOptions()
                {
                    Name = "dummy"
                }
            });

            var customerService = environment.ServiceProvider.GetRequiredService<CustomerService>();
            var customer = await customerService.CreateAsync(new CustomerCreateOptions());

            var paymentMethodService = environment.ServiceProvider.GetRequiredService<PaymentMethodService>();
            var paymentMethod = await paymentMethodService.CreateAsync(new PaymentMethodCreateOptions()
            {
                Type = "card",
                Card = new PaymentMethodCardCreateOptions()
                {
                    Cvc = "123",
                    ExpMonth = 11,
                    ExpYear = 2030,
                    Number = "4242424242424242",
                    Token = null
                }
            });

            await paymentMethodService.AttachAsync(paymentMethod.Id, new PaymentMethodAttachOptions()
            {
                Customer = customer.Id
            });

            var subscriptionService = environment.ServiceProvider.GetRequiredService<SubscriptionService>();
            var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = customer.Id,
                DefaultPaymentMethod = paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = plan.Id
                    }
                }
            });

            await subscriptionService.CancelAsync(subscription.Id, new SubscriptionCancelOptions());

            //Act
            var result = await environment.Mediator.Send(
                new GetSubscriptionByIdQuery(subscription.Id));

            //Assert
            Assert.IsNull(result);
        }
    }
}
