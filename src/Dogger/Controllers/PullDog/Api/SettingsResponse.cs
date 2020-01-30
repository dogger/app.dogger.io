namespace Dogger.Controllers.PullDog.Api
{
    public class SettingsResponse
    {
        public string? PlanId { get; set; }
        public string? ApiKey { get; set; }
        public int? PoolSize { get; set; }

        public bool IsInstalled { get; set; }
    }
}
