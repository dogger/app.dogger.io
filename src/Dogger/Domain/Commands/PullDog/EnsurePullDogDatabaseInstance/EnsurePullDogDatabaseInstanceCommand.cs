using System.Data;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance
{
    public class EnsurePullDogDatabaseInstanceCommand : IRequest<Instance>, IDatabaseTransactionRequest
    {
        public PullDogPullRequest PullRequest { get; }
        public ConfigurationFile Configuration { get; }

        public EnsurePullDogDatabaseInstanceCommand(
            PullDogPullRequest pullRequest, 
            ConfigurationFile configuration)
        {
            this.PullRequest = pullRequest;
            this.Configuration = configuration;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
