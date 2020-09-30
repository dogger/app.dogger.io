using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetSubscriptionById;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();
            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlans(await environment.Stripe.PlanBuilder.BuildAsync())
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .BuildAsync();

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
            
            var customer = await environment.Stripe.CustomerBuilder.BuildAsync();
            var subscription = await environment.Stripe.SubscriptionBuilder
                .WithPlans(await environment.Stripe.PlanBuilder.BuildAsync())
                .WithCustomer(customer)
                .WithDefaultPaymentMethod(await environment.Stripe.PaymentMethodBuilder
                    .WithCustomer(customer)
                    .BuildAsync())
                .WithCanceledState()
                .BuildAsync();

            //Act
            var result = await environment.Mediator.Send(
                new GetSubscriptionByIdQuery(subscription.Id));

            //Assert
            Assert.IsNull(result);
        }
    }
}
