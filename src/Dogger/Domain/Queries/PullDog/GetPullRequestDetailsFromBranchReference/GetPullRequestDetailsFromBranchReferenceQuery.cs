using Dogger.Domain.Models;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromBranchReference
{
    public class GetPullRequestDetailsFromBranchReferenceQuery : IRequest<PullRequest?>
    {
        public PullDogRepository Repository { get; }
        public string BranchReference { get; }

        public GetPullRequestDetailsFromBranchReferenceQuery(
            PullDogRepository repository,
            string branchReference)
        {
            this.Repository = repository;
            this.BranchReference = branchReference;
        }
    }
}
