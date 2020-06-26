using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class Instance
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        [NotLogged]
        public Cluster Cluster { get; set; }
        public Guid ClusterId { get; set; }

        [NotLogged]
        public PullDogPullRequest? PullDogPullRequest { get; set; }

        public string Name { get; set; }

        public string PlanId { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public Instance()
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }

}
