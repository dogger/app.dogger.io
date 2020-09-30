namespace Dogger.Domain.Controllers.Plans
{
    public class PlanResponse
    {
        public string Id { get; set; } = null!;

        public int PriceInHundreds { get; set; }

        public int RamSizeInMegabytes { get; set; }
        public int CpuCount { get; set; }
        public int Power { get; set; }
        public int DiskSizeInGigabytes { get; set; }
        public int TransferPerMonthInGigabytes { get; set; }

        public PullDogPlanResponse[] PullDogPlans { get; set; } = null!;
    }
}
