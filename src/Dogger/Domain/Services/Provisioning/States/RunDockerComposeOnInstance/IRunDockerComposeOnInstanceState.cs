using System.Collections.Generic;
using Destructurama.Attributed;

namespace Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance
{
    public interface IRunDockerComposeOnInstanceState : IProvisioningState
    {
        string? IpAddress { get; }
        string[]? DockerComposeYmlFilePaths { get; }

        [NotLogged]
        IDictionary<string, string>? BuildArguments { get; }
    }
}
