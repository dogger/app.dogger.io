using System.Data;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Events.ServerDeploymentFailed
{
    public class ServerDeploymentFailedEvent : IRequest, IDatabaseTransactionRequest
    {
        public string Reason { get; }
        public string FileListDump { get; }
        public string InstanceName { get; }

        public ServerDeploymentFailedEvent(
            string instanceName,
            string reason,
            string fileListDump)
        {
            this.InstanceName = instanceName;
            this.Reason = reason;
            this.FileListDump = fileListDump;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
