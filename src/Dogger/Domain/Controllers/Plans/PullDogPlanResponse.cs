namespace Dogger.Domain.Controllers.Plans
{
    public class PullDogPlanResponse
    {
        public PullDogPlanResponse(string id, int priceInHundreds, int poolSize)
        {
            this.Id = id;
            this.PriceInHundreds = priceInHundreds;
            this.PoolSize = poolSize;
        }

        public string Id { get; set; }
        public int PriceInHundreds { get; set; }
        public int PoolSize { get; set; }
    }
}
