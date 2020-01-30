using System.Collections.Generic;

namespace Dogger.Infrastructure.Docker.Yml
{
    public interface IDockerComposeParser
    {
        IReadOnlyCollection<ExposedPort> GetExposedHostPorts();
        IReadOnlyCollection<string> GetEnvironmentFilePaths();
        IReadOnlyCollection<string> GetVolumePaths();
        IReadOnlyCollection<string> GetDockerfilePaths();
    }
}
