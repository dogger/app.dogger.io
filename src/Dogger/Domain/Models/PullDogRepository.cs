using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class PullDogRepository
    {
        public Guid Id { get; set; }

        [NotLogged]
        public PullDogSettings PullDogSettings { get; set; }
        public Guid PullDogSettingsId { get; set; }

        [NotLogged]
        public List<PullDogPullRequest> PullRequests { get; set; }

        public string Handle { get; set; }

        public PullDogRepository()
        {
            PullRequests = new List<PullDogPullRequest>();
        }
    }
}
