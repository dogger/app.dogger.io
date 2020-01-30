using System;
using System.Data;
using System.Diagnostics;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioned
{
    public class RegisterInstanceAsProvisionedCommand : IRequest, IDatabaseTransactionRequest
    {
        public string InstanceName { get; }
        public Guid? UserId { get; set; }

        [DebuggerStepThrough]
        public RegisterInstanceAsProvisionedCommand(
            string instanceName)
        {
            InstanceName = instanceName;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
