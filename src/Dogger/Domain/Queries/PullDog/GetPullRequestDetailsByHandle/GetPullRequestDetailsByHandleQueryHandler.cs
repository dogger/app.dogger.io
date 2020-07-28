using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle
{
    public class GetPullRequestDetailsByHandleQueryHandler : IRequestHandler<GetPullRequestDetailsByHandleQuery, PullRequest?>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GetPullRequestDetailsByHandleQueryHandler(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<PullRequest?> Handle(
            GetPullRequestDetailsByHandleQuery request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Handle))
                throw new InvalidOperationException("The branch reference was not specified.");

            var pullDogRepository = request.Repository;
            var installationId = pullDogRepository.GitHubInstallationId;
            if (installationId == null)
                throw new InvalidOperationException("Installation ID not found.");

            var repositoryId = long.Parse(pullDogRepository.Handle, CultureInfo.InvariantCulture);
            var pullRequestNumber = int.Parse(request.Handle, CultureInfo.InvariantCulture);

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(installationId.Value);
            return await client.PullRequest.Get(repositoryId, pullRequestNumber);
        }
    }
}

