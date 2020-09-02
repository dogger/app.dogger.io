using System;
using Dogger.Tests.TestHelpers.Builders.Stripe;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace Dogger.Tests.TestHelpers.Environments
{
    public class StripeEnvironmentContext
    {
        private readonly IServiceProvider serviceProvider;

        public SubscriptionService SubscriptionService => this.serviceProvider.GetRequiredService<SubscriptionService>();
        public TestSubscriptionBuilder SubscriptionBuilder => new TestSubscriptionBuilder(SubscriptionService);

        public TestPlanBuilder PlanBuilder => new TestPlanBuilder(this.serviceProvider.GetRequiredService<PlanService>());
        public TestCustomerBuilder CustomerBuilder => new TestCustomerBuilder(this.serviceProvider.GetRequiredService<CustomerService>());
        public TestPaymentMethodBuilder PaymentMethodBuilder => new TestPaymentMethodBuilder(this.serviceProvider.GetRequiredService<PaymentMethodService>());

        public StripeEnvironmentContext(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }
    }
}
