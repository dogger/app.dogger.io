using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class InstanceResponse
    {
        public string Id { get; set; } = null!;

        public int CpuCount { get; set; }

        public int RamSizeInMegabytes { get; set; }

        public int TransferPerMonthInGigabytes { get; set; }

        public int DiskSizeInGigabytes { get; set; }

        public string PublicIpAddressV4 { get; set; } = null!;
        public string PublicIpAddressV6 { get; set; } = null!;

        public string PrivateIpAddress { get; set; } = null!;
    }
}
