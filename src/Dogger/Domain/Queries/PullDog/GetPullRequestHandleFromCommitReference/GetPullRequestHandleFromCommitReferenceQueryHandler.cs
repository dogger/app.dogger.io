using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestHandleFromCommitReference
{
    public class GetPullRequestHandleFromCommitReferenceQueryHandler : IRequestHandler<GetPullRequestHandleFromCommitReferenceQuery, string?>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GetPullRequestHandleFromCommitReferenceQueryHandler(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<string?> Handle(GetPullRequestHandleFromCommitReferenceQuery request, CancellationToken cancellationToken)
        {
            var pullDogRepository = request.Repository;
            var installationId = pullDogRepository.PullDogSettings.GitHubInstallationId;
            if (installationId == null)
                throw new InvalidOperationException("Installation ID not found.");

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(installationId.Value);

            var repositoryId = long.Parse(pullDogRepository.Handle, CultureInfo.InvariantCulture);
            var repository = await client.Repository.Get(repositoryId);

            var pullRequestsResponse = await client.Search.SearchIssues(
                new SearchIssuesRequest($"{request.CommitReference} type:pr state:open repo:{repository.Owner.Login}/{repository.Name}"));
            var pullRequest = pullRequestsResponse.Items.SingleOrDefault();
            return pullRequest.Number.ToString(CultureInfo.InvariantCulture);
        }
    }
}
