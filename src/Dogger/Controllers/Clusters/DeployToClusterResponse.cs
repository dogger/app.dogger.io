using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class DeployToClusterResponse
    {
        public string? JobId { get; set; }
    }
}
