using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class InstanceResponse
    {
        public string? Id
        {
            get; set;
        }

        public int CpuCount
        {
            get; set;
        }

        public int RamSizeInMegabytes
        {
            get; set;
        }

        public int TransferPerMonthInGigabytes
        {
            get; set;
        }

        public int DiskSizeInGigabytes
        {
            get; set;
        }

        public string? PublicIpAddressV4 { get; set; }
        public string? PublicIpAddressV6 { get; set; }

        public string? PrivateIpAddress { get; set; }
    }
}
