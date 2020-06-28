using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class PullRequestReadyPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event => "pull_request";

        public PullRequestReadyPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            if (payload.PullRequest?.Draft == true)
                return false;

            if (payload.PullRequest?.State != "open")
                return false;

            return
                payload.Action == "ready_for_review" ||
                payload.Action == "synchronize" ||
                payload.Action == "opened" ||
                payload.Action == "reopened";
        }

        public async Task HandleAsync(WebhookPayloadContext context)
        {
            if (context.Payload.PullRequest?.User?.Type == "Bot")
            {
                await this.mediator.Send(new UpsertPullRequestCommentCommand(
                    context.PullRequest,
                    "I won't create a test environment for this pull request, since it was created by another bot. You can still use commands to provision an environment."));
                return;
            }

            await this.mediator.Send(new ProvisionPullDogEnvironmentCommand(
                context.PullRequest.Handle,
                context.Repository));
        }
    }
}
