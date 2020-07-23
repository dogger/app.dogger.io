using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class PullRequestClosedPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event => "pull_request";

        public PullRequestClosedPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return 
                payload.Action == "closed" &&
                payload.PullRequest?.User?.Type != "Bot";
        }

        public async Task HandleAsync(WebhookPayloadContext context)
        {
            await mediator.Send(
                new DeleteInstanceByPullRequestCommand(
                    context.Repository.Handle,
                    context.PullRequest.Handle,
                    InitiatorType.System));
        }
    }
}
