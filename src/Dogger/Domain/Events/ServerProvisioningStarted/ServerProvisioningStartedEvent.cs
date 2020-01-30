using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Events.ServerProvisioningStarted
{
    public class ServerProvisioningStartedEvent : IRequest
    {
        public Instance Instance { get; }

        public ServerProvisioningStartedEvent(
            Instance instance)
        {
            this.Instance = instance;
        }
    }
}
