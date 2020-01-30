using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Domain.Models
{
    [ExcludeFromCodeCoverage]
    public class AmazonUser
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        [NotLogged]
        public byte[] EncryptedAccessKeyId { get; set; }

        [NotLogged]
        public byte[] EncryptedSecretAccessKey { get; set; }

        [NotLogged]
        public User? User { get; set; }
        public Guid? UserId { get; set; }
    }
}
