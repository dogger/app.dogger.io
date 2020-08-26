using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;
using Dogger.Domain.Services.PullDog;



namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class PullDogPullRequest
    {
        public Guid Id { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        [NotLogged]
        public PullDogRepository PullDogRepository { get; set; } = null!;
        public Guid PullDogRepositoryId { get; set; }

        public ConfigurationFileOverride? ConfigurationOverride { get; set; }

        [NotLogged]
        public Instance? Instance { get; set; }
        public Guid? InstanceId { get; set; }

        public string Handle { get; set; } = null!;

        public PullDogPullRequest()
        {
            CreatedAtUtc = DateTime.UtcNow;
        }
    }
}
