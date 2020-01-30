using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Docker.Yml;

namespace Dogger.Domain.Services.PullDog
{

    public class PullDogFileCollectorFactory : IPullDogFileCollectorFactory
    {
        private readonly IPullDogRepositoryClientFactory factory;
        private readonly IDockerComposeParserFactory dockerComposeParserFactory;

        public PullDogFileCollectorFactory(
            IPullDogRepositoryClientFactory factory,
            IDockerComposeParserFactory dockerComposeParserFactory)
        {
            this.factory = factory;
            this.dockerComposeParserFactory = dockerComposeParserFactory;
        }

        public async Task<IPullDogFileCollector> CreateAsync(PullDogPullRequest pullRequest)
        {
            var client = await factory.CreateAsync(pullRequest);
            return new PullDogFileCollector(client, dockerComposeParserFactory);
        }
    }
}
