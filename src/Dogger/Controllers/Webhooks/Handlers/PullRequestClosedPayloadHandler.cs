using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class PullRequestClosedPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;

        public PullRequestClosedPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return 
                payload.Action == "closed" && 
                payload.PullRequest?.State == "closed";
        }

        public async Task HandleAsync(WebhookPayloadContext context)
        {
            await mediator.Send(
                new DeleteInstanceByPullRequestCommand(
                    context.Repository.Handle,
                    context.PullRequest.Handle));
        }
    }
}
