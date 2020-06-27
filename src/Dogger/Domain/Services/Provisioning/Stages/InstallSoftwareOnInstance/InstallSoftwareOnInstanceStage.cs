using Dogger.Domain.Services.Provisioning.Instructions;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance
{
    public class InstallSoftwareOnInstanceStage : IInstallSoftwareOnInstanceStage
    {
        public void AddInstructionsTo(IBlueprintBuilder blueprintBuilder)
        {
            CollectInstallDockerInstructions(blueprintBuilder
                .AddGroup("Installing Docker Engine"));

            CollectInstallDockerComposeInstructions(blueprintBuilder
                .AddGroup("Installing Docker Compose"));

            CollectConfigureDockerDaemonInstructions(blueprintBuilder
                .AddGroup("Configuring Docker Daemon"));

            CollectConfigurePostInstallInstructions(blueprintBuilder
                .AddGroup("Finalizing Docker installation"));

            CollectSetSystemConfigurationValueInstructions(
                blueprintBuilder, 
                "vm.max_map_count", "262144");
        }

        private static void CollectSetSystemConfigurationValueInstructions(
            IBlueprintBuilder instructionCollector,
            string key,
            string value)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo sysctl -w {key}={value}"));

            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo bash -c \"echo '{key}={value}' >> /etc/sysctl.conf\""));
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/engine/install/linux-postinstall/
        /// </summary>
        private static void CollectConfigurePostInstallInstructions(IBlueprintBuilder instructionCollector)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "sudo usermod -aG docker $USER"));

            //verify that we can run docker without root access.
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "docker --version"));
        }

        private static void CollectConfigureDockerDaemonInstructions(IBlueprintBuilder instructionCollector)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                "sudo systemctl enable docker"));
        }

        /// <summary>
        /// These sets of commands are taken from https://docs.docker.com/compose/install/
        /// </summary>
        private static void CollectInstallDockerComposeInstructions(IBlueprintBuilder instructionCollector)
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
        private static void CollectInstallDockerInstructions(IBlueprintBuilder instructionCollector)
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
            IBlueprintBuilder instructionCollector,
            RetryPolicy retryPolicy,
            string[] commands)
        {
            foreach (var command in commands)
            {
                instructionCollector.AddInstruction(new SshInstruction(
                    retryPolicy,
                    command));
            }
        }
    }
}
