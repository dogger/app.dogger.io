using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest
{
    public class RemoveLabelFromGitHubPullRequestCommandHandler : IRequestHandler<RemoveLabelFromGitHubPullRequestCommand, Unit>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public RemoveLabelFromGitHubPullRequestCommandHandler(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<Unit> Handle(
            RemoveLabelFromGitHubPullRequestCommand request,
            CancellationToken cancellationToken)
        {
            var repository = request.PullRequest.PullDogRepository;

            if (repository?.GitHubInstallationId == null)
                throw new InvalidOperationException("Could not fetch the GitHub installation ID.");

            if (!long.TryParse(repository.Handle, out var repositoryId))
                throw new InvalidOperationException("Invalid repository handle.");

            if (!int.TryParse(request.PullRequest.Handle, out var pullRequestNumber))
                throw new InvalidOperationException("Invalid pull request handle.");

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(
                repository.GitHubInstallationId.Value);
            if (client == null)
                return Unit.Value;
            
            await client
                .Issue
                .Labels
                .RemoveFromIssue(repositoryId, pullRequestNumber, request.Label);

            return Unit.Value;
        }
    }
}

