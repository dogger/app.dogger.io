using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Destructurama.Attributed;
using Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts;
using Dogger.Domain.Events.ServerDeploymentCompleted;
using Dogger.Domain.Events.ServerDeploymentFailed;
using Dogger.Domain.Queries.Instances.GetNecessaryInstanceFirewallPorts;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Instructions;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Infrastructure.Ssh;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance
{
    public class RunDockerComposeOnInstanceStage : IRunDockerComposeOnInstanceStage
    {
        public void CollectInstructions(IInstructionGroupCollector instructionCollector)
        {
            CollectClearExistingFilesInstructions(instructionCollector);

            instructionCollector.CollectInstructionWithSignal("docker-compose");
            instructionCollector.CollectInstructionWithSignal("open-firewall");
        }

        private static void CollectClearExistingFilesInstructions(
            IInstructionGroupCollector instructionCollector)
        {
            CollectRemoveDirectoryInstructions(instructionCollector, "dogger");
            CollectEnsureDirectoryInstructions(instructionCollector, "dogger");
        }

        private static void CollectRemoveDirectoryInstructions(
            IInstructionGroupCollector instructionCollector,
            string path)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo rm ./{path} -rf"));
        }

        private static void CollectEnsureDirectoryInstructions(
            IInstructionGroupCollector instructionCollector,
            string path)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"mkdir -m 777 -p ./{path}"));

            CollectSetUserPermissionsOnPathInstructions(instructionCollector, path);
        }

        private static void CollectSetUserPermissionsOnPathInstructions(
            IInstructionGroupCollector instructionCollector,
            string fileName)
        {
            instructionCollector.CollectInstruction(new SshInstruction(
                RetryPolicy.AllowRetries,
                $"sudo chmod 777 ./{fileName}"));
        }
    }
}
