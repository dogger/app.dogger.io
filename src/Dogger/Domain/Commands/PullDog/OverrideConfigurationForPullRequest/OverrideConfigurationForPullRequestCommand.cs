using System;
using System.Data;
using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest
{
    public class OverrideConfigurationForPullRequestCommand : IRequest, IDatabaseTransactionRequest
    {
        public Guid PullDogPullRequestId { get; }

        public ConfigurationFileOverride ConfigurationOverride { get; }

        public OverrideConfigurationForPullRequestCommand(
            Guid pullDogPullRequestId, 
            ConfigurationFileOverride configurationOverride)
        {
            this.PullDogPullRequestId = pullDogPullRequestId;
            this.ConfigurationOverride = configurationOverride;
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }
}
