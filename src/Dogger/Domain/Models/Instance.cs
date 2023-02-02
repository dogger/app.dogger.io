using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;



namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class Instance
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }

        [NotLogged]
        public Cluster Cluster { get; set; } = null!;
        public Guid ClusterId { get; set; }

        [NotLogged]
        public PullDogPullRequest? PullDogPullRequest { get; set; }

        public string Name { get; set; } = null!;

        public string PlanId { get; set; } = null!;

        public bool? IsProvisioned { get; set; }

        public DateTime? ExpiresAtUtc { get; set; }

        public Instance()
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
