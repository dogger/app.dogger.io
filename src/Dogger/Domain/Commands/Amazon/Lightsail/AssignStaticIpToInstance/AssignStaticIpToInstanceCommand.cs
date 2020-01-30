using MediatR;

namespace Dogger.Domain.Commands.Amazon.Lightsail.AssignStaticIpToInstance
{
    public class AssignStaticIpToInstanceCommand : IRequest
    {
        public string StaticIpName { get; }
        public string InstanceName { get; }

        public AssignStaticIpToInstanceCommand(
            string instanceName,
            string staticIpName)
        {
            this.InstanceName = instanceName;
            this.StaticIpName = staticIpName;
        }
    }
}
