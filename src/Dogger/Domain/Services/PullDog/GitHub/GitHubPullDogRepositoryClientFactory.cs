using System;
using System.Globalization;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.GitHub;
using Serilog;

namespace Dogger.Domain.Services.PullDog.GitHub
{
    public class GitHubPullDogRepositoryClientFactory : IPullDogRepositoryClientFactory
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GitHubPullDogRepositoryClientFactory(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<IPullDogRepositoryClient> CreateAsync(
            PullDogPullRequest pullRequest)
        {
            var repository = pullRequest.PullDogRepository;
            if (repository?.GitHubInstallationId == null)
                throw new InvalidOperationException("The GitHub installation ID could not be determined.");

            var installationId = repository.GitHubInstallationId.Value;
            var gitHubClient = await this.gitHubClientFactory.CreateInstallationClientAsync(installationId);

            var gitHubPullRequest = await gitHubClient.PullRequest.Get(
                long.Parse(repository.Handle, CultureInfo.InvariantCulture),
                int.Parse(pullRequest.Handle, CultureInfo.InvariantCulture));

            return new GitHubPullDogRepositoryClient(
                gitHubClient,
                gitHubPullRequest.Head);
        }
    }
}
