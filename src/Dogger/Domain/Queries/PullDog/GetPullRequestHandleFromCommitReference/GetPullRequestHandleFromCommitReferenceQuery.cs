using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestHandleFromCommitReference
{
    public class GetPullRequestHandleFromCommitReferenceQuery : IRequest<string?>
    {
        public PullDogRepository Repository { get; }
        public string CommitReference { get; }

        public GetPullRequestHandleFromCommitReferenceQuery(
            PullDogRepository repository,
            string commitReference)
        {
            this.Repository = repository;
            this.CommitReference = commitReference;
        }
    }
}
