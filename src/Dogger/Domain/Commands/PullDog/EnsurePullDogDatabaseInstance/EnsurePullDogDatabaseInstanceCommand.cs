using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance
{
    public class EnsurePullDogDatabaseInstanceCommand : IRequest<Instance>, IDatabaseTransactionRequest
    {
        public PullDogPullRequest PullRequest { get; }

        public EnsurePullDogDatabaseInstanceCommand(
            PullDogPullRequest pullRequest)
        {
            this.PullRequest = pullRequest;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
