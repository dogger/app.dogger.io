using System.Data;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogRepository
{
    public class EnsurePullDogRepositoryCommand : IRequest<PullDogRepository>, IDatabaseTransactionRequest
    {
        public PullDogSettings PullDogSettings { get; }

        public string RepositoryHandle { get; }

        public EnsurePullDogRepositoryCommand(
            PullDogSettings pullDogSettings,
            string repositoryHandle)
        {
            this.PullDogSettings = pullDogSettings;
            this.RepositoryHandle = repositoryHandle;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
