using System.Collections.Generic;

namespace Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance
{
    public interface IRunDockerComposeOnInstanceState : IProvisioningState
    {
        string? IpAddress { get; }
        string[]? DockerComposeYmlFilePaths { get; }
        IDictionary<string, string>? BuildArguments { get; }
    }
}
