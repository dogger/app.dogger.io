using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.UpsertPullRequestComment
{
    public class UpsertPullRequestCommentCommand : IRequest
    {
        public PullDogPullRequest PullRequest { get; }

        public string Content { get; }

        public UpsertPullRequestCommentCommand(
            PullDogPullRequest pullRequest,
            string content)
        {
            this.PullRequest = pullRequest;
            this.Content = content;
        }
    }
}
