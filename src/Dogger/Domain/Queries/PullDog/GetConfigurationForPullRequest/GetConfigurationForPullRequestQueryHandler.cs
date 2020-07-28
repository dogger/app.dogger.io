using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Services.PullDog;
using MediatR;
using Serilog;

namespace Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest
{
    public class GetConfigurationForPullRequestQueryHandler : IRequestHandler<GetConfigurationForPullRequestQuery, ConfigurationFile>
    {
        private readonly IPullDogFileCollectorFactory pullDogFileCollectorFactory;
        private readonly ILogger logger;

        public GetConfigurationForPullRequestQueryHandler(
            IPullDogFileCollectorFactory pullDogFileCollectorFactory,
            ILogger logger)
        {
            this.pullDogFileCollectorFactory = pullDogFileCollectorFactory;
            this.logger = logger;
        }

        public async Task<ConfigurationFile> Handle(GetConfigurationForPullRequestQuery request, CancellationToken cancellationToken)
        {
            var client = await this.pullDogFileCollectorFactory.CreateAsync(request.PullRequest);

            var configuration = await client.GetConfigurationFileAsync();
            if (configuration == null)
            {
                configuration = new ConfigurationFile();
                logger.Information("No configuration file was found, a default will be used.");
            }

            var configurationOverride = request.PullRequest.ConfigurationOverride;
            if (configurationOverride == null)
                return configuration;

            ApplyOverridesToConfiguration(
                configurationOverride, 
                configuration);

            return configuration;
        }

        private static void ApplyOverridesToConfiguration(
            ConfigurationFileOverride configurationOverride, 
            ConfigurationFile configuration)
        {
            configuration.IsLazy = false;

            if (configurationOverride.BuildArguments != null)
                configuration.BuildArguments = configurationOverride.BuildArguments;

            if (configurationOverride.ConversationMode != default)
                configuration.ConversationMode = configurationOverride.ConversationMode;

            if (configurationOverride.Expiry != default)
                configuration.Expiry = configurationOverride.Expiry;

            if (configurationOverride.Label != default)
                configuration.Label = configurationOverride.Label;
        }
    }
}
