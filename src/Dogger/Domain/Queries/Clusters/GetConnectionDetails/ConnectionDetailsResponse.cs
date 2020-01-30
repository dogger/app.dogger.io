using System.Collections.Generic;
using Dogger.Infrastructure.Docker.Yml;

namespace Dogger.Domain.Queries.Clusters.GetConnectionDetails
{
    public class ConnectionDetailsResponse
    {
        public ConnectionDetailsResponse(
            string ipAddress,
            string? hostName,
            IEnumerable<ExposedPort> ports)
        {
            this.IpAddress = ipAddress;
            this.HostName = hostName;
            this.Ports = ports;
        }

        public string IpAddress { get; }
        public string? HostName { get; }

        public IEnumerable<ExposedPort> Ports { get; }
    }
}
