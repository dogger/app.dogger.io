using Dogger.Domain.Services.Provisioning.Instructions;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance
{
    public class RunDockerComposeOnInstanceStage : IRunDockerComposeOnInstanceStage
    {
        public void AddInstructionsTo(IBlueprintBuilder blueprintBuilder)
        {
            CollectClearExistingFilesInstructions(blueprintBuilder);

            blueprintBuilder.AddInstructionWithSignal("docker-compose");
            blueprintBuilder.AddInstructionWithSignal("open-firewall");
        }

        private static void CollectClearExistingFilesInstructions(
            IBlueprintBuilder instructionCollector)
        {
            CollectRemoveDirectoryInstructions(instructionCollector, "dogger");
            CollectEnsureDirectoryInstructions(instructionCollector, "dogger");
        }

        private static void CollectRemoveDirectoryInstructions(
            IBlueprintBuilder instructionCollector,
            string path)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo rm ./{path} -rf"));
        }

        private static void CollectEnsureDirectoryInstructions(
            IBlueprintBuilder instructionCollector,
            string path)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"mkdir -m 777 -p ./{path}"));

            CollectSetUserPermissionsOnPathInstructions(instructionCollector, path);
        }

        private static void CollectSetUserPermissionsOnPathInstructions(
            IBlueprintBuilder instructionCollector,
            string fileName)
        {
            instructionCollector.AddInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo chmod 777 ./{fileName}"));
        }
    }
}
