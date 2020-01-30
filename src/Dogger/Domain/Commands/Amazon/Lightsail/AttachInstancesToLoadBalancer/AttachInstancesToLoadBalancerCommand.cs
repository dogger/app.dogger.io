using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.AttachInstancesToLoadBalancer
{
    public class AttachInstancesToLoadBalancerCommand : IRequest
    {
        public string LoadBalancerName { get; }
        public IEnumerable<string> InstanceNames { get; }

        public AttachInstancesToLoadBalancerCommand(
            string loadBalancerName,
            IEnumerable<string> instanceNames)
        {
            this.LoadBalancerName = loadBalancerName;
            this.InstanceNames = instanceNames;
        }
    }
}
