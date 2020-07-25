using Dogger.Domain.Models;
using Dogger.Infrastructure.Mediatr.Tracing;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.UpsertPullRequestComment
{
    public class UpsertPullRequestCommentCommand : IRequest, ITraceableRequest
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

        public string? TraceId { get; set; }
    }
}
