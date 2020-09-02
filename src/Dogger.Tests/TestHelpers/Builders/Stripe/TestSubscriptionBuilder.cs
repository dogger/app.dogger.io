using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe;

namespace Dogger.Tests.TestHelpers.Builders.Stripe
{
    public class TestSubscriptionBuilder
    {
        private readonly SubscriptionService subscriptionService;

        private Customer customer;
        private PaymentMethod paymentMethod;
        private Plan plan;

        private bool shouldCancel;

        public TestSubscriptionBuilder(
            SubscriptionService subscriptionService)
        {
            this.subscriptionService = subscriptionService;
        }

        public TestSubscriptionBuilder WithCustomer(Customer customer)
        {
            this.customer = customer;
            return this;
        }

        public TestSubscriptionBuilder WithDefaultPaymentMethod(PaymentMethod paymentMethod)
        {
            this.paymentMethod = paymentMethod;
            return this;
        }

        public TestSubscriptionBuilder WithPlan(Plan plan)
        {
            this.plan = plan;
            return this;
        }

        public TestSubscriptionBuilder WithCanceledState()
        {
            this.shouldCancel = true;
            return this;
        }

        public async Task<Subscription> BuildAsync()
        {
            var subscription = await this.subscriptionService.CreateAsync(new SubscriptionCreateOptions()
            {
                Customer = this.customer.Id,
                DefaultPaymentMethod = this.paymentMethod.Id,
                Items = new List<SubscriptionItemOptions>()
                {
                    new SubscriptionItemOptions()
                    {
                        Plan = this.plan.Id
                    }
                }
            });

            if (this.shouldCancel)
            {
                await this.subscriptionService.CancelAsync(subscription.Id, new SubscriptionCancelOptions());
            }

            return subscription;
        }
    }
}
