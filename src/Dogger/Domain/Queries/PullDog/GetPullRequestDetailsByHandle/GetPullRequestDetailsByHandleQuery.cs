using Dogger.Domain.Models;
using MediatR;
using Octokit;

namespace Dogger.Domain.Queries.PullDog.GetPullRequestDetailsByHandle
{
    public class GetPullRequestDetailsByHandleQuery : IRequest<PullRequest?>
    {
        public GetPullRequestDetailsByHandleQuery(
            PullDogRepository repository,
            string handle)
        {
            this.Repository = repository;
            this.Handle = handle;
        }

        public PullDogRepository Repository { get; }
        public string Handle { get; }
    }
}
