using Dogger.Domain.Models;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromCommitReference
{
    public class GetPullRequestDetailsFromCommitReferenceQuery : IRequest<PullRequest?>
    {
        public PullDogRepository Repository { get; }
        public string CommitReference { get; }

        public GetPullRequestDetailsFromCommitReferenceQuery(
            PullDogRepository repository,
            string commitReference)
        {
            this.Repository = repository;
            this.CommitReference = commitReference;
        }
    }
}
