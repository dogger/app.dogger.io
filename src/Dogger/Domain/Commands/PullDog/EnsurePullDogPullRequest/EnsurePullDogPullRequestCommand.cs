using System.Data;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest
{
    public class EnsurePullDogPullRequestCommand : IRequest<PullDogPullRequest>, IDatabaseTransactionRequest
    {
        public PullDogRepository Repository { get; }

        public string PullRequestHandle { get; }

        public EnsurePullDogPullRequestCommand(
            PullDogRepository repository,
            string pullRequestHandle)
        {
            this.Repository = repository;
            this.PullRequestHandle = pullRequestHandle;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
