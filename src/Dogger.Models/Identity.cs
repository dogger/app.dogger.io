using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class Identity
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        [NotLogged]
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }
    }
}
