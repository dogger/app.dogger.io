using System;
using System.Data;
using System.Diagnostics;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioning
{
    public class RegisterInstanceAsProvisioningCommand : IRequest, IDatabaseTransactionRequest
    {
        public string InstanceName { get; }
        public Guid? UserId { get; set; }

        [DebuggerStepThrough]
        public RegisterInstanceAsProvisioningCommand(
            string instanceName)
        {
            this.InstanceName = instanceName;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
