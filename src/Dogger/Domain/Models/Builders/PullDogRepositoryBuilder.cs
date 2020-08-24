using System;
using System.Collections.Generic;

namespace Dogger.Domain.Models.Builders
{
    public class PullDogRepositoryBuilder : ModelBuilder<PullDogRepository>
    {
        private Guid id;

        private EntityReference<PullDogSettings>? pullDogSettings;

        private IEnumerable<PullDogPullRequest> pullRequests;

        private long? gitHubInstallationId;

        private string? handle;

        public PullDogRepositoryBuilder()
        {
            pullRequests = Array.Empty<PullDogPullRequest>();
        }

        public PullDogRepositoryBuilder WithId(Guid value)
        {
            this.id = value;
            return this;
        }

        public PullDogRepositoryBuilder WithPullDogSettings(PullDogSettings value)
        {
            this.pullDogSettings = value;
            return this;
        }

        public PullDogRepositoryBuilder WithPullRequests(params PullDogPullRequest[] value)
        {
            this.pullRequests = value;
            return this;
        }

        public PullDogRepositoryBuilder WithPullDogSettings(Guid value)
        {
            this.pullDogSettings = new EntityReference<PullDogSettings>(value);
            return this;
        }

        public PullDogRepositoryBuilder WithGitHubInstallationId(long? value)
        {
            this.gitHubInstallationId = value;
            return this;
        }

        public PullDogRepositoryBuilder WithHandle(string value)
        {
            this.handle = value;
            return this;
        }

        public override PullDogRepository Build()
        {
            var repository = new PullDogRepository()
            {
                GitHubInstallationId = gitHubInstallationId,
                Handle = handle ?? throw new InvalidOperationException("Handle not specified."),
                Id = id,
                PullDogSettings = pullDogSettings?.Reference!,
                PullDogSettingsId = pullDogSettings?.Id ?? default
            };
            repository.PullRequests.AddRange(pullRequests);

            return repository;
        }
    }
}
