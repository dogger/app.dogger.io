using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class DeployToClusterStateFlow : IProvisioningStateFlow
    {
        public string InstanceName { get; }
        public string[] DockerComposeYmlContents { get; }
        public IEnumerable<IDockerAuthenticationArguments>? Authentication { get; set; }
        public IEnumerable<InstanceDockerFile>? Files { get; set; }
        public IDictionary<string, string>? BuildArguments { get; set; }

        public DeployToClusterStateFlow(
            string instanceName,
            string[] dockerComposeYmlContents)
        {
            this.InstanceName = instanceName;
            this.DockerComposeYmlContents = dockerComposeYmlContents;
        }

        public async Task<IProvisioningState> GetInitialStateAsync(InitialStateContext context)
        {
            var amazonInstance = await context.Mediator.Send(
                new GetLightsailInstanceByNameQuery(InstanceName));
            if (amazonInstance == null)
                throw new InvalidOperationException("Instance was not found.");

            return context.StateFactory.Create<RunDockerComposeOnInstanceState>(state =>
            {
                state.BuildArguments = BuildArguments;
                state.DockerComposeYmlContents = DockerComposeYmlContents;
                state.InstanceName = InstanceName;
                state.IpAddress = amazonInstance.PublicIpAddress;
                state.Authentication = Authentication;
                state.Files = this.Files;
            });
        }

        public async Task<IProvisioningState?> GetNextStateAsync(NextStateContext context)
        {
            return null;
        }
    }
}
