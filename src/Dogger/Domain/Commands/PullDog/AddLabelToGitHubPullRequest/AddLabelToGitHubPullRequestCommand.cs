using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.AddLabelToGitHubPullRequest
{
    public class AddLabelToGitHubPullRequestCommand : IRequest<Unit>
    {
        public PullDogPullRequest PullRequest { get; }

        public string Label { get; }

        public AddLabelToGitHubPullRequestCommand(
            PullDogPullRequest pullRequest,
            string label)
        {
            this.PullRequest = pullRequest;
            this.Label = label;
        }
    }
}
