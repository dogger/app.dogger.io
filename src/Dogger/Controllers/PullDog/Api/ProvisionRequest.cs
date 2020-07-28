using System.Diagnostics.CodeAnalysis;
using Dogger.Domain.Services.PullDog;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Controllers.PullDog.Api
{
    [ExcludeFromCodeCoverage]
    public class ProvisionRequest
    {
        public string RepositoryHandle { get; set; }

        public string? PullRequestHandle { get; set; }
        public string? BranchReference { get; set; }

        public string ApiKey { get; set; }

        public ConfigurationFileOverride? Configuration { get; set; }
    }
}
