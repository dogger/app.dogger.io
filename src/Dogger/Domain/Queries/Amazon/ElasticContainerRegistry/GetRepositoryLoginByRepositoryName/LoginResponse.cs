using Destructurama.Attributed;

namespace Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName
{
    public class RepositoryLoginResponse
    {
        [NotLogged]
        public string Username { get; }

        [NotLogged]
        public string Password { get; }

        public RepositoryLoginResponse(
            string username,
            string password)
        {
            this.Username = username;
            this.Password = password;
        }
    }
}
