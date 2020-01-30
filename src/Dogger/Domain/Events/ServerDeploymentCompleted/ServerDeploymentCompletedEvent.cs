using MediatR;

namespace Dogger.Domain.Events.ServerDeploymentCompleted
{
    public class ServerDeploymentCompletedEvent : IRequest
    {
        public string InstanceName { get; }

        public ServerDeploymentCompletedEvent(
            string instanceName)
        {
            this.InstanceName = instanceName;
        }
    }
}
