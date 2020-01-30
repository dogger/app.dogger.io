using Dogger.Domain.Models;

namespace Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName
{
    public class RepositoryResponse
    {
        public string Name { get; }
        public string HostName { get; }

        public AmazonUser ReadUser { get; }
        public AmazonUser WriteUser { get; }

        public RepositoryResponse(
            string name,
            string hostName,
            AmazonUser readUser,
            AmazonUser writeUser)
        {
            this.Name = name;
            this.HostName = hostName;
            this.ReadUser = readUser;
            this.WriteUser = writeUser;
        }
    }
}
