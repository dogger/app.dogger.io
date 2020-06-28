using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories
{
    public class AddPullDogToGitHubRepositoriesCommand : IRequest, IDatabaseTransactionRequest
    {
        public long GitHubInstallationId { get; }
        public PullDogSettings PullDogSettings { get; }
        public long[] GitHubRepositoryIds { get; }

        public AddPullDogToGitHubRepositoriesCommand(
            long gitHubInstallationId,
            PullDogSettings pullDogSettings,
            long[] gitHubRepositoryIds)
        {
            this.GitHubInstallationId = gitHubInstallationId;
            this.PullDogSettings = pullDogSettings;
            this.GitHubRepositoryIds = gitHubRepositoryIds;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
