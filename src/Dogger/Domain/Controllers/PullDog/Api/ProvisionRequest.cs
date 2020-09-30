using System.Diagnostics.CodeAnalysis;
using Dogger.Domain.Services.PullDog;

namespace Dogger.Domain.Controllers.PullDog.Api
{
    [ExcludeFromCodeCoverage]
    public class ProvisionRequest
    {
        public string RepositoryHandle { get; set; } = null!;

        public string? PullRequestHandle { get; set; }
        public string? BranchReference { get; set; }

        public string ApiKey { get; set; } = null!;

        public ConfigurationFileOverride? Configuration { get; set; }
    }
}
