using Amazon.Lightsail.Model;

namespace Dogger.Domain.Queries.Plans.GetSupportedPlans
{
    public class Plan
    {
        public string Id { get; }
        public int PriceInHundreds { get; }

        public Bundle Bundle { get; }
        public PullDogPlan[] PullDogPlans { get; }

        public Plan(
            string id,
            int priceInHundreds,
            Bundle bundle,
            PullDogPlan[] pullDogPlans)
        {
            this.Id = id;
            this.PriceInHundreds = priceInHundreds;
            this.Bundle = bundle;
            this.PullDogPlans = pullDogPlans;

            foreach (var pullDogPlan in pullDogPlans)
                pullDogPlan.DoggerPlan = this;
        }
    }
}
