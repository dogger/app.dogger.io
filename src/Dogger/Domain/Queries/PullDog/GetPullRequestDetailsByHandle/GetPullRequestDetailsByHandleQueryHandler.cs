using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Octokit;
using Serilog;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle
{
    public class GetPullRequestDetailsByHandleQueryHandler : IRequestHandler<GetPullRequestDetailsByHandleQuery, PullRequest?>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly ILogger logger;

        public GetPullRequestDetailsByHandleQueryHandler(
            IGitHubClientFactory gitHubClientFactory,
            ILogger logger)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.logger = logger;
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

            try
            {
                return await client.PullRequest.Get(repositoryId, pullRequestNumber);
            }
            catch (NotFoundException)
            {
                return null;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, 
                    "An error occured while fetching pull request details for {RepositoryId} and {PullRequestNumber}.",
                    repositoryId,
                    pullRequestNumber);
                throw;
            }
        }
    }
}

