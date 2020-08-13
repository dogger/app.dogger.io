using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options.GitHub
{
    [ExcludeFromCodeCoverage]
    public class GitHubPullDogOptions
    {
        [NotLogged]
        public string? PrivateKeyPath { get; set; }

        [NotLogged]
        public int? AppIdentifier { get; set; }

        [NotLogged]
        public string? WebhookSecret { get; set; }

        [NotLogged]
        public string? ClientId { get; set; }

        [NotLogged]
        public string? ClientSecret { get; set; }
    }
}
