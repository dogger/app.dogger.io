using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Stripe;

namespace Dogger.Tests.TestHelpers.Builders.Stripe
{
    public enum PlanInterval
    {
        Month
    }

    public enum PlanCurrency
    {
        Usd
    }

    public class TestPlanBuilder
    {
        private readonly PlanService planService;

        private PlanInterval interval;
        private PlanCurrency currency;
        private int amountInHundreds;
        private string id;

        public TestPlanBuilder(
            PlanService planService)
        {
            this.planService = planService;

            WithInterval(PlanInterval.Month);
            WithCurrency(PlanCurrency.Usd);
            WithAmountInHundreds(1_00);
        }

        public TestPlanBuilder WithInterval(PlanInterval value)
        {
            this.interval = value;
            return this;
        }

        public TestPlanBuilder WithCurrency(PlanCurrency value)
        {
            this.currency = value;
            return this;
        }

        public TestPlanBuilder WithAmountInHundreds(int value)
        {
            this.amountInHundreds = value;
            return this;
        }

        public TestPlanBuilder WithId(string value)
        {
            this.id = value;
            return this;
        }

        public async Task<Plan> BuildAsync()
        {
            return await planService.CreateAsync(new PlanCreateOptions()
            {
                Id = this.id,
                Interval = this.interval.ToString().ToLower(),
                Currency = this.currency.ToString().ToLower(),
                Amount = this.amountInHundreds,
                Product = new PlanProductCreateOptions()
                {
                    Name = Guid.NewGuid().ToString()
                }
            });
        }
    }
}
