using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest
{
    public class GetAvailableClusterFromPullRequestQuery : IRequest<Cluster>
    {
        public PullDogPullRequest PullRequest { get; }

        public GetAvailableClusterFromPullRequestQuery(
            PullDogPullRequest pullRequest)
        {
            this.PullRequest = pullRequest;
        }
    }
}
