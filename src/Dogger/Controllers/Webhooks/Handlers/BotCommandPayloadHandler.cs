using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class BotCommandPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event { get; }
        public string Action => "created";

        public BotCommandPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return
                payload.Action == "created" &&
                payload.Issue?.PullRequest != null &&
                payload.Comment != null;
        }

        public async Task HandleAsync(WebhookPayloadContext context)
        {
            var payload = context.Payload;
            var text = payload.Comment?.Body;

            switch (text)
            {
                case "@pull-dog down":
                    await this.mediator.Send(new DeleteInstanceByPullRequestCommand(
                        context.Repository.Handle,
                        context.PullRequest.Handle));
                    break;

                case "@pull-dog up":
                    await this.mediator.Send(new ProvisionPullDogEnvironmentCommand(
                        context.PullRequest.Handle,
                        context.Repository));
                    break;

                case null:
                    throw new InvalidOperationException("No text found.");
            }
        }
    }
}
