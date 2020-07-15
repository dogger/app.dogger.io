using Dogger.Domain.Models;
using Dogger.Domain.Services.PullDog;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest
{
    public class GetConfigurationForPullRequestQuery : IRequest<ConfigurationFile>
    {
        public PullDogPullRequest PullRequest { get; }

        public GetConfigurationForPullRequestQuery(
            PullDogPullRequest pullRequest)
        {
            this.PullRequest = pullRequest;
        }
    }
}
