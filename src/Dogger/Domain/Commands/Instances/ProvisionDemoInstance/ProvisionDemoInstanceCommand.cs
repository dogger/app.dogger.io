using System;
using Dogger.Domain.Services.Provisioning;
using MediatR;

namespace Dogger.Domain.Commands.Instances.ProvisionDemoInstance
{
    public class ProvisionDemoInstanceCommand : IRequest<IProvisioningJob>
    {
        public Guid? AuthenticatedUserId { get; set; }
    }
}
