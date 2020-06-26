using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class PullDogSettings
    {
        public Guid Id { get; set; }

        [NotLogged]
        public User User { get; set; }
        public Guid UserId { get; set; }

        public string PlanId { get; set; }
        public int PoolSize { get; set; }

        [NotLogged]
        public byte[] EncryptedApiKey { get; set; }

        [NotLogged]
        public List<PullDogRepository> Repositories { get; set; }

        [NotLogged]
        public List<Blueprint> PoolBlueprints { get; set; }

        public PullDogSettings()
        {
            this.Repositories = new List<PullDogRepository>();
            this.PoolBlueprints = new List<Blueprint>();
        }
    }
}
