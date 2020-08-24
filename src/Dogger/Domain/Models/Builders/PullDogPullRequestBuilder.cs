using System;
using Dogger.Domain.Services.PullDog;

namespace Dogger.Domain.Models.Builders
{
    public class PullDogPullRequestBuilder : ModelBuilder<PullDogPullRequest>
    {
        private Guid id;

        private string? handle;

        private DateTime? createdAtUtc;

        private EntityReference<PullDogRepository>? pullDogRepository;
        private EntityReference<Instance>? instance;

        private ConfigurationFileOverride? configurationOverride;

        public PullDogPullRequestBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public PullDogPullRequestBuilder WithHandle(string value)
        {
            this.handle = value;
            return this;
        }

        public PullDogPullRequestBuilder WithCreatedDate(DateTime value)
        {
            this.createdAtUtc = value;
            return this;
        }

        public PullDogPullRequestBuilder WithPullDogRepository(PullDogRepository value)
        {
            this.pullDogRepository = value;
            return this;
        }

        public PullDogPullRequestBuilder WithInstance(Instance value)
        {
            this.instance = value;
            return this;
        }

        public PullDogPullRequestBuilder WithConfigurationOverride(ConfigurationFileOverride value)
        {
            this.configurationOverride = value;
            return this;
        }

        public override PullDogPullRequest Build()
        {
            if (instance == null)
                throw new InvalidOperationException("Instance not specified.");

            if (pullDogRepository == null)
                throw new InvalidOperationException("Pull Dog repository not specified.");

            return new PullDogPullRequest()
            {
                ConfigurationOverride = configurationOverride,
                CreatedAtUtc = createdAtUtc ?? DateTime.UtcNow,
                Handle = handle ?? throw new InvalidOperationException("No handle specified."),
                Id = id,
                Instance = instance,
                InstanceId = instance,
                PullDogRepository = pullDogRepository,
                PullDogRepositoryId = pullDogRepository
            };
        }
    }
}
