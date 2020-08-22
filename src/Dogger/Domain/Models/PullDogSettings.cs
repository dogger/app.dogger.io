using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;



namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class PullDogSettings
    {
        public Guid Id { get; set; }

        [NotLogged]
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }

        public string PlanId { get; set; } = null!;
        public int PoolSize { get; set; }

        [NotLogged]
        public byte[] EncryptedApiKey { get; set; } = null!;

        [NotLogged]
        public List<PullDogRepository> Repositories { get; set; }

        public PullDogSettings()
        {
            this.Repositories = new List<PullDogRepository>();
        }
    }
}
