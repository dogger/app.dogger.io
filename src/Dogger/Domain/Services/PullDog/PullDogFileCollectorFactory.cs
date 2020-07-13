using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Docker.Yml;
using Serilog;

namespace Dogger.Domain.Services.PullDog
{

    public class PullDogFileCollectorFactory : IPullDogFileCollectorFactory
    {
        private readonly IPullDogRepositoryClientFactory factory;
        private readonly IDockerComposeParserFactory dockerComposeParserFactory;
        private readonly ILogger logger;

        public PullDogFileCollectorFactory(
            IPullDogRepositoryClientFactory factory,
            IDockerComposeParserFactory dockerComposeParserFactory,
            ILogger logger)
        {
            this.factory = factory;
            this.dockerComposeParserFactory = dockerComposeParserFactory;
            this.logger = logger;
        }

        public async Task<IPullDogFileCollector> CreateAsync(PullDogPullRequest pullRequest)
        {
            var client = await factory.CreateAsync(pullRequest);
            return new PullDogFileCollector(
                client, 
                dockerComposeParserFactory, 
                logger);
        }
    }
}
