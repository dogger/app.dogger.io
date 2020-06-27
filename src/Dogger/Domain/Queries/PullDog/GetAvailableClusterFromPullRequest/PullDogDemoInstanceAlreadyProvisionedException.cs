using System;
using Dogger.Domain.Services.PullDog;

namespace Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest
{
    public class PullDogDemoInstanceAlreadyProvisionedException : Exception
    {
        public PullRequestDetails[] OffendingPullRequests { get; }

        public PullDogDemoInstanceAlreadyProvisionedException(
            PullRequestDetails[] offendingPullRequests)
        {
            this.OffendingPullRequests = offendingPullRequests;
        }
    }
}
