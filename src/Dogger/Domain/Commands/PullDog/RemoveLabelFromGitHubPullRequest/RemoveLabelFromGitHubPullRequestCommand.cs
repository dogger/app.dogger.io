using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest
{
    public class RemoveLabelFromGitHubPullRequestCommand : IRequest<Unit>
    {
        public PullDogPullRequest PullRequest { get; }

        public string Label { get; }

        public RemoveLabelFromGitHubPullRequestCommand(
            PullDogPullRequest pullRequest,
            string label)
        {
            this.PullRequest = pullRequest;
            this.Label = label;
        }
    }
}
