using System.Data;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment
{
    public class ProvisionPullDogEnvironmentCommand : IRequest, IDatabaseTransactionRequest
    {
        public string PullRequestHandle { get; }
        public PullDogRepository Repository { get; }

        public ConfigurationFileOverride? ConfigurationOverride { get; set; }

        public ProvisionPullDogEnvironmentCommand(
            string pullRequestHandle,
            PullDogRepository repository)
        {
            this.PullRequestHandle = pullRequestHandle;
            this.Repository = repository;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
