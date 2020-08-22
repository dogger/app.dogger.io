using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class InstanceResponse
    {
        public InstanceResponse(
            string id, 
            int cpuCount, 
            int ramSizeInMegabytes, 
            int transferPerMonthInGigabytes, 
            int diskSizeInGigabytes, 
            string publicIpAddressV4, 
            string publicIpAddressV6, 
            string privateIpAddress)
        {
            this.Id = id;
            this.CpuCount = cpuCount;
            this.RamSizeInMegabytes = ramSizeInMegabytes;
            this.TransferPerMonthInGigabytes = transferPerMonthInGigabytes;
            this.DiskSizeInGigabytes = diskSizeInGigabytes;
            this.PublicIpAddressV4 = publicIpAddressV4;
            this.PublicIpAddressV6 = publicIpAddressV6;
            this.PrivateIpAddress = privateIpAddress;
        }

        public string Id { get; set; }

        public int CpuCount { get; set; }

        public int RamSizeInMegabytes { get; set; }

        public int TransferPerMonthInGigabytes { get; set; }

        public int DiskSizeInGigabytes { get; set; }

        public string PublicIpAddressV4 { get; set; }
        public string PublicIpAddressV6 { get; set; }

        public string PrivateIpAddress { get; set; }
    }
}
