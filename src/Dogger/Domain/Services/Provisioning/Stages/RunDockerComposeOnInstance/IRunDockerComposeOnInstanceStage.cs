using System.Collections.Generic;
using Dogger.Domain.Services.Provisioning.Arguments;

namespace Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance
{
    public interface IRunDockerComposeOnInstanceStage : IProvisioningStage
    {
        string? IpAddress { get; set; }
        string? InstanceName { get; set; }
        string[]? DockerComposeYmlContents { get; set; }
        IEnumerable<InstanceDockerFile>? Files { get; set; }
        IEnumerable<IDockerAuthenticationArguments>? Authentication { get; }
        IDictionary<string, string>? BuildArguments { get; set; }
    }
}
