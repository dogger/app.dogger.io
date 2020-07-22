using System;
using Dogger.Domain.Services.PullDog;

namespace Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest
{
    public class PullDogDemoInstanceAlreadyProvisionedException : Exception
    {
        public PullRequestDetails LatestOffendingPullRequest { get; }

        public PullDogDemoInstanceAlreadyProvisionedException(
            PullRequestDetails offendingPullRequest)
        {
            this.LatestOffendingPullRequest = offendingPullRequest;
        }
    }
}
