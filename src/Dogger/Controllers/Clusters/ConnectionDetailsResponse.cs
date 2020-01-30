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
            string? hostName,
            IEnumerable<ExposedPort> ports)
        {
            IpAddress = ipAddress;
            HostName = hostName;
            Ports = ports;
        }

        public string IpAddress { get; }
        public string? HostName { get; }

        public IEnumerable<ExposedPort> Ports { get; }
    }
}
