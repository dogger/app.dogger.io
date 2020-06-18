using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class PullRequestClosedPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event => "pull_request";
        public string Action => "closed";

        public PullRequestClosedPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
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
