using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class AmazonUser
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        [NotLogged]
        public byte[] EncryptedAccessKeyId { get; set; } = null!;

        [NotLogged]
        public byte[] EncryptedSecretAccessKey { get; set; } = null!;

        [NotLogged]
        public User? User { get; set; }
        public Guid? UserId { get; set; }
    }
}
