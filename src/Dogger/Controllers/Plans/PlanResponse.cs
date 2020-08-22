namespace Dogger.Controllers.Plans
{
    public class PlanResponse
    {
        public PlanResponse(
            string id, 
            int priceInHundreds, 
            int ramSizeInMegabytes, 
            int cpuCount, 
            int power, 
            int diskSizeInGigabytes, 
            int transferPerMonthInGigabytes, 
            PullDogPlanResponse[] pullDogPlans)
        {
            this.Id = id;
            this.PriceInHundreds = priceInHundreds;
            this.RamSizeInMegabytes = ramSizeInMegabytes;
            this.CpuCount = cpuCount;
            this.Power = power;
            this.DiskSizeInGigabytes = diskSizeInGigabytes;
            this.TransferPerMonthInGigabytes = transferPerMonthInGigabytes;
            this.PullDogPlans = pullDogPlans;
        }

        public string Id { get; set; }

        public int PriceInHundreds { get; set; }

        public int RamSizeInMegabytes { get; set; }
        public int CpuCount { get; set; }
        public int Power { get; set; }
        public int DiskSizeInGigabytes { get; set; }
        public int TransferPerMonthInGigabytes { get; set; }

        public PullDogPlanResponse[] PullDogPlans { get; set; }
    }
}
