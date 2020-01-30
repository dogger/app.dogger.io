using System;
using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Domain.Services.Provisioning.Arguments
{
    [ExcludeFromCodeCoverage]
    public class DockerAuthenticationArguments : IDockerAuthenticationArguments
    {
        [NotLogged]
        public string Username { get; }

        [NotLogged]
        public string Password { get; }

        [NotLogged]
        public string? RegistryHostName { get; set; }

        public DockerAuthenticationArguments(
            string username,
            string password)
        {
            if (string.IsNullOrEmpty(username))
                throw new ArgumentException("Username must be specified.", nameof(username));

            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password must be specified.", nameof(password));

            Username = username;
            Password = password;
        }
    }
}
