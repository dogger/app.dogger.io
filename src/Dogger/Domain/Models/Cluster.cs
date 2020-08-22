using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class Cluster
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }

        [NotLogged]
        public User? User { get; set; }
        public Guid? UserId { get; set; }

        [NotLogged]
        public List<Instance> Instances { get; set; }

        public Cluster()
        {
            Instances = new List<Instance>();
        }
    }
}
