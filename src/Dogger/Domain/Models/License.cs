using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;



namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class License
    {
        public Guid Id { get; set; }

        public byte[] EncryptedToken { get; set; } = null!;

        [NotLogged]
        public User User { get; set; } = null!;
        public Guid UserId { get; set; }
    }
}
