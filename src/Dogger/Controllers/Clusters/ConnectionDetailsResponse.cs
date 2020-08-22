using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Dogger.Infrastructure.Docker.Yml;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class ConnectionDetailsResponse
    {
        public ConnectionDetailsResponse(
            string ipAddress,
            IEnumerable<ExposedPort> ports)
        {
            IpAddress = ipAddress;
            Ports = ports;
        }

        public string IpAddress { get; }
        public string? HostName { get; set; }

        public IEnumerable<ExposedPort> Ports { get; }
    }
}
