using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioned;
using Dogger.Infrastructure.Ssh;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup
{
    public sealed class CompleteInstanceSetupState : SshInstanceState, ICompleteInstanceSetupState
    {

        private readonly IMediator mediator;

        public override string Description { get; }

        public override string? IpAddress { get; set; }

        public string? InstanceName { get; set; }

        public Guid? UserId { get; set; }

        public CompleteInstanceSetupState(
            ISshClientFactory sshClientFactory,
            IMediator mediator) : base(sshClientFactory)
        {
            this.Description = "Completing instance setup";

            this.mediator = mediator;
        }

        protected override async Task<ProvisioningStateUpdateResult> OnUpdateAsync(ISshClient sshClient)
        {
            if (this.InstanceName == null)
                throw new InvalidOperationException("Instance name has not been set.");

            await this.mediator.Send(
                new RegisterInstanceAsProvisionedCommand(
                    this.InstanceName)
                {
                    UserId = this.UserId
                });

            return ProvisioningStateUpdateResult.Succeeded;
        }
    }
}
