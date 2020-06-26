using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Domain.Services.Provisioning.Instructions;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Infrastructure.Ssh;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance
{
    public class InstallSoftwareOnInstanceStage : IInstallSoftwareOnInstanceStage
    {
        public Guid? UserId { get; set; }
        public string? InstanceName { get; set; }

        public void CollectInstructions(IInstructionGroupCollector instructionCollector)
        {
            CollectInstallDockerInstructions(instructionCollector.CollectGroup("Installing Docker Engine"));
            CollectInstallDockerComposeInstructions(instructionCollector.CollectGroup("Installing Docker Compose"));
            CollectConfigureDockerDaemonInstructions(instructionCollector.CollectGroup("Configuring Docker Daemon"));
            CollectConfigurePostInstallInstructions(instructionCollector.CollectGroup("Finalizing Docker installation"));
            CollectSetSystemConfigurationValueInstructions(instructionCollector, "vm.max_map_count", "262144");
        }

        private static void CollectSetSystemConfigurationValueInstructions(
            IInstructionGroupCollector instructionCollector,
            string key,
            string value)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo sysctl -w {key}={value}"));

            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo bash -c \"echo '{key}={value}' >> /etc/sysctl.conf\""));
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/engine/install/linux-postinstall/
        /// </summary>
        private static void CollectConfigurePostInstallInstructions(IInstructionGroupCollector instructionCollector)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "sudo usermod -aG docker $USER"));

            //verify that we can run docker without root access.
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "docker --version"));
        }

        private static void CollectConfigureDockerDaemonInstructions(IInstructionGroupCollector instructionCollector)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "sudo systemctl enable docker"));
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/compose/install/
        /// </summary>
        private static void CollectInstallDockerComposeInstructions(IInstructionGroupCollector instructionCollector)
        {
            CollectSshCommands(
                instructionCollector,
                RetryPolicy.AllowRetries,
                new[]
                {
                    "sudo curl -L \"https://github.com/docker/compose/releases/download/1.25.3/docker-compose-$(uname -s)-$(uname -m)\" -o /usr/local/bin/docker-compose",
                    "sudo chmod +x /usr/local/bin/docker-compose"
                });
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/install/linux/docker-ce/ubuntu/.
        /// </summary>
        private static void CollectInstallDockerInstructions(IInstructionGroupCollector instructionCollector)
        {
            //SET UP THE REPOSITORY
            CollectSshCommands(
                instructionCollector,
                RetryPolicy.AllowRetries,
                new[]
                {
                    "sudo apt-get update",
                    "echo '* libraries/restart-without-asking boolean true' | sudo debconf-set-selections",
                    "sudo apt-get -y install apt-transport-https ca-certificates curl gnupg-agent software-properties-common",
                    "curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo apt-key add -",
                    "sudo add-apt-repository \"deb [arch=amd64] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable\""
                });

            //INSTALL DOCKER ENGINE - COMMUNITY
            CollectSshCommands(
                instructionCollector,
                RetryPolicy.AllowRetries,
                new[]
                {
                    "sudo apt-get update",
                    "sudo apt-get -y install docker-ce docker-ce-cli containerd.io"
                });
        }

        private static void CollectSshCommands(
            IInstructionGroupCollector instructionCollector,
            RetryPolicy retryPolicy,
            string[] commands)
        {
            foreach (var command in commands)
            {
                instructionCollector.CollectInstruction(new SshInstruction(
                    retryPolicy,
                    command));
            }
        }

        public void Dispose()
        {
        }
    }
}
