using System;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ssh;
using Renci.SshNet.Common;

namespace Dogger.Domain.Services.Provisioning.States
{
    public abstract class SshInstanceState : IProvisioningState
    {
        private readonly ISshClientFactory sshClientFactory;

        private ISshClient? sshClient;

        public abstract string Description { get; }
        public abstract string? IpAddress { get; set; }

        protected SshInstanceState(
            ISshClientFactory sshClientFactory)
        {
            this.sshClientFactory = sshClientFactory;
        }

        public virtual void Dispose()
        {
            this.sshClient?.Dispose();
        }

        protected abstract Task<ProvisioningStateUpdateResult> OnUpdateAsync(ISshClient sshClient);

        public async Task<ProvisioningStateUpdateResult> UpdateAsync()
        {
            if (this.sshClient == null)
                throw new InvalidOperationException("SSH state not initialized.");

            return await OnUpdateAsync(this.sshClient);
        }

        public async Task InitializeAsync()
        {
            if (IpAddress == null)
                throw new InvalidOperationException("IP address must be set.");

            try
            {
                this.sshClient = await this.sshClientFactory.CreateForLightsailInstanceAsync(IpAddress);
            }
            catch (SshConnectionException ex)
            {
                throw new StateUpdateException("Could not connect to SSH.", ex);
            }
        }
    }
}
