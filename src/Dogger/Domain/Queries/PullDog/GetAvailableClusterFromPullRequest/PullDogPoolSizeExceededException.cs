using System;
using Dogger.Domain.Services.PullDog;

namespace Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest
{
    public class PullDogPoolSizeExceededException : Exception
    {
        public PullRequestDetails[] OffendingPullRequests { get; }

        public PullDogPoolSizeExceededException(
            PullRequestDetails[] offendingPullRequests)
        {
            this.OffendingPullRequests = offendingPullRequests;
        }
    }
}
