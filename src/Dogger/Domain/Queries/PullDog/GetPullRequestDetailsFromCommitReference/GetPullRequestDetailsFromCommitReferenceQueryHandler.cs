using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromCommitReference
{
    public class GetPullRequestDetailsFromCommitReferenceQueryHandler : IRequestHandler<GetPullRequestDetailsFromCommitReferenceQuery, PullRequest?>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GetPullRequestDetailsFromCommitReferenceQueryHandler(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<PullRequest?> Handle(GetPullRequestDetailsFromCommitReferenceQuery request, CancellationToken cancellationToken)
        {
            var pullDogRepository = request.Repository;
            var installationId = pullDogRepository.GitHubInstallationId;
            if (installationId == null)
                throw new InvalidOperationException("Installation ID not found.");

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(installationId.Value);

            var repositoryId = long.Parse(pullDogRepository.Handle, CultureInfo.InvariantCulture);
            var repository = await client.Repository.Get(repositoryId);

            var pullRequestsResponse = await client.Search.SearchIssues(
                new SearchIssuesRequest($"{request.CommitReference} type:pr state:open repo:{repository.Owner.Login}/{repository.Name}"));
            var issue = pullRequestsResponse.Items.SingleOrDefault();
            return issue?.PullRequest;
        }
    }
}
