using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class PullDogRepository
    {
        public Guid Id { get; set; }

        [NotLogged]
        public PullDogSettings PullDogSettings { get; set; } = null!;
        public Guid PullDogSettingsId { get; set; }

        [NotLogged]
        public List<PullDogPullRequest> PullRequests { get; set; }

        public long? GitHubInstallationId { get; set; }

        public string Handle { get; set; } = null!;

        public PullDogRepository()
        {
            PullRequests = new List<PullDogPullRequest>();
        }
    }
}
