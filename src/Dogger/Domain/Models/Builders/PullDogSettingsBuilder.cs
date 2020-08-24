using System;
using System.Collections.Generic;

namespace Dogger.Domain.Models.Builders
{
    public class PullDogSettingsBuilder : ModelBuilder<PullDogSettings>
    {
        private Guid id;

        private string? planId;
        private byte[]? encryptedApiKey;

        private int poolSize;

        private IEnumerable<PullDogRepository> repositories;

        private EntityReference<User>? user;

        public PullDogSettingsBuilder()
        {
            repositories = Array.Empty<PullDogRepository>();
        }

        public PullDogSettingsBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public PullDogSettingsBuilder WithPlanId(string value)
        {
            this.planId = value;
            return this;
        }

        public PullDogSettingsBuilder WithEncryptedApiKey(byte[] value)
        {
            this.encryptedApiKey = value;
            return this;
        }

        public PullDogSettingsBuilder WithPoolSize(int value)
        {
            this.poolSize = value;
            return this;
        }

        public PullDogSettingsBuilder WithUser(User value)
        {
            this.user = value;
            return this;
        }

        public PullDogSettingsBuilder WithUser(Guid value)
        {
            this.user = new EntityReference<User>(value);
            return this;
        }

        public PullDogSettingsBuilder WithRepositories(params PullDogRepository[] value)
        {
            this.repositories = value;
            return this;
        }

        public override PullDogSettings Build()
        {
            if (user == null)
                throw new InvalidOperationException("No user specified.");

            var settings = new PullDogSettings()
            {
                EncryptedApiKey = encryptedApiKey ?? throw new InvalidOperationException("No API key specified."),
                Id = id,
                PlanId = planId ?? throw new InvalidOperationException("No plan ID specified."),
                PoolSize = poolSize,
                User = user.Reference!,
                UserId = user.Id
            };
            settings.Repositories.AddRange(repositories);

            return settings;
        }
    }
}
