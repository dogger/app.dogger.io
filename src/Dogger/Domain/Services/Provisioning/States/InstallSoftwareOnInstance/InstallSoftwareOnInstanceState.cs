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
                $"sudo sysctl -w {key}={value}");

            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
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
                "sudo usermod -aG docker $USER");

            //verify that we can run docker without root access.
            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                "docker --version");
        }

        //private async Task InstallKubernetesOnNodeAsync(
        //    ISshClient sshClient, 
        //    InstanceType instanceType)
        //{
        //    await InstallKubernetesToolsAsync(sshClient);

        //    switch (instanceType)
        //    {
        //        case InstanceType.KubernetesControlPlane:
        //            await InitializeKubernetesControlPlaneNodeAsync(sshClient);
        //            break;

        //        case InstanceType.KubernetesWorker:
        //            await InitializeKubernetesWorkerNodeAsync(sshClient);
        //            break;

        //        default:
        //            throw new InvalidOperationException("Unknown instance type.");
        //    }
        //}

        //private async Task InitializeKubernetesWorkerNodeAsync(ISshClient sshClient)
        //{
        //    throw new NotImplementedException("Kubernetes worker support has not been created yet.");
        //}

        private async Task ConfigureDockerDaemonAsync(ISshClient sshClient)
        {
            this.description = "Configuring Docker Daemon";

            await sshClient.ExecuteCommandAsync(
                SshRetryPolicy.AllowRetries,
                "sudo systemctl enable docker");
        }

        //private async Task InstallKubernetesToolsAsync(ISshClient sshClient)
        //{
        //    this.description = "Installing Kubernetes";

        //    await RunCommandsAsync(
        //        sshClient,
        //        SshRetryPolicy.AllowRetries,
        //        new[]
        //        {
        //            "sudo apt-get update",
        //            "sudo apt-get install -y apt-transport-https curl",
        //            "sudo curl -s https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo apt-key add -",
        //            "sudo cat <<EOF | sudo tee /etc/apt/sources.list.d/kubernetes.list\ndeb https://apt.kubernetes.io/ kubernetes-xenial main\nEOF",
        //            "sudo apt-get update",
        //            "sudo apt-get install -y kubelet kubeadm kubectl",
        //            "sudo apt-mark hold kubelet kubeadm kubectl"
        //        });
        //}

        ///// <summary>
        ///// From: https://kubernetes.io/docs/setup/production-environment/tools/kubeadm/create-cluster-kubeadm/#installing-kubeadm-on-your-hosts
        ///// </summary>
        //private async Task InitializeKubernetesControlPlaneNodeAsync(ISshClient sshClient)
        //{
        //    throw new NotImplementedException("Kubernetes control plane support has not been created yet.");
        //}

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/compose/install/
        /// </summary>
        private async Task InstallDockerComposeAsync(ISshClient sshClient)
        {
            this.description = "Installing Docker Compose";

            await RunCommandsAsync(
                sshClient,
                SshRetryPolicy.AllowRetries,
                new[]
                {
                    "sudo curl -L \"https://github.com/docker/compose/releases/download/1.25.3/docker-compose-$(uname -s)-$(uname -m)\" -o /usr/local/bin/docker-compose",
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
                SshRetryPolicy.AllowRetries,
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
                SshRetryPolicy.AllowRetries,
                new[]
                {
                    "sudo apt-get update",
                    "sudo apt-get -y install docker-ce docker-ce-cli containerd.io"
                });
        }

        private static async Task RunCommandsAsync(
            ISshClient sshClient,
            SshRetryPolicy retryPolicy,
            string[] commands)
        {
            foreach (var command in commands)
            {
                await sshClient.ExecuteCommandAsync(
                    retryPolicy,
                    command);
            }
        }
    }
}
