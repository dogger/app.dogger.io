using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace Dogger.Domain.Commands.Instances.SetInstanceExpiry
{
    public class SetInstanceExpiryCommand : IRequest<Unit>
    {
        public string InstanceName { get; }
        public DateTime Expiry { get; }

        public SetInstanceExpiryCommand(
            string instanceName,
            DateTime expiry)
        {
            this.InstanceName = instanceName;
            this.Expiry = expiry;
        }
    }
}
