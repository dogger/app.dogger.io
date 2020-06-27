using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class DeployToClusterStageFlow : IProvisioningStageFlow
    {
        public string InstanceName { get; }
        public string[] DockerComposeYmlContents { get; }
        public IEnumerable<IDockerAuthenticationArguments>? Authentication { get; set; }
        public IEnumerable<InstanceDockerFile>? Files { get; set; }
        public IDictionary<string, string>? BuildArguments { get; set; }

        public DeployToClusterStageFlow(
            string instanceName,
            string[] dockerComposeYmlContents)
        {
            this.InstanceName = instanceName;
            this.DockerComposeYmlContents = dockerComposeYmlContents;
        }

        public IProvisioningStage GetInitialState(IProvisioningStateFactory stateFactory)
        {
            return stateFactory.Create<RunDockerComposeOnInstanceStage>();
        }

        public IProvisioningStage? GetNextState(
            IProvisioningStage currentStage,
            IProvisioningStateFactory stateFactory)
        {
            return null;
        }
    }
}
