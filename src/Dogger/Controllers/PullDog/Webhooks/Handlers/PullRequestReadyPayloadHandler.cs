using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using MediatR;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
{
    public class 
        PullRequestReadyPayloadHandler : IWebhookPayloadHandler
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
            var pullRequest = payload.PullRequest;
            if (pullRequest == null)
                return false;

            var isPullRequestReady = PullRequestReadinessHelper.IsReady(
                pullRequest.Draft,
                pullRequest.State,
                pullRequest.User?.Type);
            if (!isPullRequestReady)
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
