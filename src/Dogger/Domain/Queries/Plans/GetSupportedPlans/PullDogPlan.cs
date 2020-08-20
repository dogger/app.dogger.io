namespace Dogger.Domain.Queries.Plans.GetSupportedPlans
{
    public class PullDogPlan
    {
        public string Id { get; }

        public int PriceInHundreds { get; }
        public int PoolSize { get; }

        public Plan? DoggerPlan { get; set; }

        public PullDogPlan(
            string id,
            int priceInHundreds,
            int poolSize)
        {
            this.Id = id;
            this.PriceInHundreds = priceInHundreds;
            this.PoolSize = poolSize;
        }
    }
}
