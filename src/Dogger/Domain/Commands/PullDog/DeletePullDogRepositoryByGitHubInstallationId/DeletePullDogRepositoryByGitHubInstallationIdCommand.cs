using System.Data;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeletePullDogRepositoryByGitHubInstallationId
{
    public class DeletePullDogRepositoryByGitHubInstallationIdCommand : IRequest, IDatabaseTransactionRequest
    {
        public long GitHubInstallationId { get; }

        public DeletePullDogRepositoryByGitHubInstallationIdCommand(
            long gitHubInstallationId)
        {
            this.GitHubInstallationId = gitHubInstallationId;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
