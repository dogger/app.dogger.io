using System.Data;
using Dogger.Domain.Services.Provisioning;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Setup.Domain.Commands.ProvisionDogfeedInstance
{
    public class ProvisionDogfeedInstanceCommand : IRequest<IProvisioningJob>, IDatabaseTransactionRequest
    {
        public string InstanceName { get; }

        public ProvisionDogfeedInstanceCommand(
            string instanceName)
        {
            this.InstanceName = instanceName;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
