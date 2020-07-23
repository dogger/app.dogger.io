using System.Data;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest
{
    public class DeleteInstanceByPullRequestCommand : IRequest, IDatabaseTransactionRequest
    {
        public string PullRequestHandle { get; }
        public string RepositoryHandle { get; }

        public InitiatorType InitiatedBy { get; }

        public DeleteInstanceByPullRequestCommand(
            string repositoryHandle,
            string pullRequestHandle,
            InitiatorType initiatedBy)
        {
            this.RepositoryHandle = repositoryHandle;
            this.PullRequestHandle = pullRequestHandle;
            this.InitiatedBy = initiatedBy;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
