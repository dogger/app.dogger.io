using System;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ssh;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance
{
    public class InstallSoftwareOnInstanceState : SshInstanceState, IInstallSoftwareOnInstanceState
    {
        private readonly IMediator mediator;
        private string description;

        public override string? IpAddress
        {
            get; set;
        }

        public override string Description => this.description;

        public Guid? UserId { get; set; }
        public string? InstanceName { get; set; }

        public InstallSoftwareOnInstanceState(
            ISshClientFactory sshClientFactory,
            IMediator mediator) : base(sshClientFactory)
        {
            this.mediator = mediator;

            this.description = "Configuring instance";
        }

        protected override async Task<ProvisioningStateUpdateResult> OnUpdateAsync(ISshClient sshClient)
        {
            if (this.IpAddress == null)
                throw new InvalidOperationException($"Must provide IP address to {nameof(InstallSoftwareOnInstanceState)}");

            await InstallDockerAsync(sshClient);
            await InstallDockerComposeAsync(sshClient);
            await ConfigureDockerDaemonAsync(sshClient);
            await ConfigurePostInstallAsync(sshClient);

            await SetSystemConfigurationValueAsync(sshClient, "vm.max_map_count", "262144");

            return ProvisioningStateUpdateResult.Succeeded;
        }

        private static async Task SetSystemConfigurationValueAsync(
            ISshClient sshClient,
            string key,
            string value)
        {
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                $"sudo sysctl -w {key}={value}");

            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                $"sudo bash -c \"echo '{key}={value}' >> /etc/sysctl.conf\"");
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/engine/install/linux-postinstall/
        /// </summary>
        private async Task ConfigurePostInstallAsync(ISshClient sshClient)
        {
            this.description = "Configuring Docker permissions";

            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "sudo usermod -aG docker $USER");

            //verify that we can run docker without root access.
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "docker --version");
        }

        private async Task ConfigureDockerDaemonAsync(ISshClient sshClient)
        {
            this.description = "Configuring Docker Daemon";

            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                SshResponseSensitivity.ContainsNoSensitiveData,
                "sudo systemctl enable docker");
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/compose/install/
        /// </summary>
        private async Task InstallDockerComposeAsync(ISshClient sshClient)
        {
            this.description = "Installing Docker Compose";

            await RunCommandsAsync(
                sshClient,
                new[]
                {
                    "sudo curl -L \"https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)\" -o /usr/local/bin/docker-compose",
                    "sudo chmod +x /usr/local/bin/docker-compose"
                });
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/install/linux/docker-ce/ubuntu/.
        /// </summary>
        private async Task InstallDockerAsync(ISshClient sshClient)
        {
            this.description = "Installing Docker Engine";

            //SET UP THE REPOSITORY
            await RunCommandsAsync(
                sshClient,
                new[]
                {
                    "sudo apt-get update",
                    "echo '* libraries/restart-without-asking boolean true' | sudo debconf-set-selections",
                    "sudo apt-get -y install apt-transport-https ca-certificates curl gnupg-agent software-properties-common",
                    "curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -",
                    "sudo add-apt-repository \"deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable\""
                });

            //INSTALL DOCKER ENGINE - COMMUNITY
            await RunCommandsAsync(
                sshClient,
                new[]
                {
                    "sudo apt-get update",
                    "sudo apt-get -y install docker-ce docker-ce-cli containerd.io"
                });
        }

        private static async Task RunCommandsAsync(
            ISshClient sshClient,
            string[] commands)
        {
            foreach (var command in commands)
            {
                await sshClient.ExecuteCommandAsync(
                    SshRetryPolicy.AllowRetries,
                    SshResponseSensitivity.ContainsNoSensitiveData,
                    command);
            }
        }
    }
}
