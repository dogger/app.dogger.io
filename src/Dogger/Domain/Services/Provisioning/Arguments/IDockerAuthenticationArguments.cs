using Destructurama.Attributed;

namespace Dogger.Domain.Services.Provisioning.Arguments
{
    public interface IDockerAuthenticationArguments
    {
        [NotLogged]
        string Password { get; }

        [NotLogged]
        string Username { get; }

        [NotLogged]
        public string? RegistryHostName { get; }
    }
}
