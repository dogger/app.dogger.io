using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using MediatR;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
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
            await this.mediator.Send(
                new DeleteInstanceByPullRequestCommand(
                    context.Repository.Handle,
                    context.PullRequest.Handle,
                    InitiatorType.System));
        }
    }
}
