namespace Dogger.Controllers.PullDog.Api
{
    public class ChangePlanRequest
    {
        public int PoolSize { get; set; }
        public string PlanId { get; set; } = null!;
    }
}
