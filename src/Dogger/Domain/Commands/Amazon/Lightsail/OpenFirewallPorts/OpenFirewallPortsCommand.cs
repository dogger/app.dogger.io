using System.Collections.Generic;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts
{
    public class OpenFirewallPortsCommand : IRequest
    {
        public string InstanceName { get; }

        public IEnumerable<ExposedPortRange> Ports { get; }

        public OpenFirewallPortsCommand(
            string instanceName,
            IEnumerable<ExposedPortRange> ports)
        {
            this.InstanceName = instanceName;
            this.Ports = ports;
        }
    }
}
