using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class DeployToClusterRequest
    {
        public string[] DockerComposeYmlFilePaths { get; set; } = null!;
        public FileRequest[]? Files { get; set; }
    }

}
