using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class DeployToClusterRequest
    {
        public string[] DockerComposeYmlFilePaths { get; set; }
        public FileRequest[]? Files { get; set; }
    }

}
