using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
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
            await this.mediator.Send(new ProvisionPullDogEnvironmentCommand(
                context.PullRequest.Handle,
                context.Repository));
        }
    }
}
