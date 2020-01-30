using System.Data;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest
{
    public class DeleteInstanceByPullRequestCommand : IRequest, IDatabaseTransactionRequest
    {
        public string PullRequestHandle { get; }
        public string RepositoryHandle { get; }

        public DeleteInstanceByPullRequestCommand(
            string repositoryHandle,
            string pullRequestHandle)
        {
            this.RepositoryHandle = repositoryHandle;
            this.PullRequestHandle = pullRequestHandle;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
