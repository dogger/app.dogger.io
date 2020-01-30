using System.Collections.Generic;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetNecessaryInstanceFirewallPorts
{
    public class GetNecessaryInstanceFirewallPortsQuery : IRequest<IReadOnlyCollection<ExposedPortRange>>
    {
        public string InstanceName { get; }

        public GetNecessaryInstanceFirewallPortsQuery(
            string instanceName)
        {
            this.InstanceName = instanceName;
        }
    }
}
