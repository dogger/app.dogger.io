using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using MediatR;
using Microsoft.Extensions.Hosting;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class BotCommandPayloadHandler : IWebhookPayloadHandler
    {
        private readonly IMediator mediator;
        private readonly IHostEnvironment hostEnvironment;

        public string Event => "issue_comment";

        public BotCommandPayloadHandler(
            IMediator mediator,
            IHostEnvironment hostEnvironment)
        {
            this.mediator = mediator;
            this.hostEnvironment = hostEnvironment;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return payload.Action == "created";
        }

        public async Task HandleAsync(WebhookPayloadContext context)
        {
            var payload = context.Payload;
            var text = ExtractCommentTextFromPayload(payload);

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

        private string? ExtractCommentTextFromPayload(WebhookPayload payload)
        {
            var text = payload
                .Comment?
                .Body?
                .Trim();

            var environmentNameSuffix = $" {this.hostEnvironment.EnvironmentName}";
            if (text?.EndsWith(environmentNameSuffix, StringComparison.InvariantCultureIgnoreCase) == true)
            {
                text = text.Substring(0, text.LastIndexOf(
                    environmentNameSuffix, 
                    StringComparison.InvariantCultureIgnoreCase));
            }

            while (text?.Contains("  ", StringComparison.InvariantCulture) == true)
                text = text.Replace("  ", " ", StringComparison.InvariantCulture);

            text = text?
                .Trim()?
                .ToLowerInvariant();

            return text;
        }
    }
}
