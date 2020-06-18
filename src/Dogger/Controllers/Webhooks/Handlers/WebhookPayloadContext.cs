using System.Diagnostics.CodeAnalysis;
using Dogger.Domain.Models;

namespace Dogger.Controllers.Webhooks.Handlers
{
    [ExcludeFromCodeCoverage]
    public class WebhookPayloadContext
    {
        public WebhookPayload Payload { get; }
        public PullDogSettings Settings { get; }
        public PullDogRepository Repository { get; }
        public PullDogPullRequest PullRequest { get; }

        public string Event { get; }

        public WebhookPayloadContext(
            WebhookPayload payload,
            PullDogSettings settings,
            PullDogRepository repository,
            PullDogPullRequest pullRequest,
            string @event)
        {
            this.Payload = payload;
            this.Settings = settings;
            this.Repository = repository;
            this.PullRequest = pullRequest;
            this.Event = @event;
        }
    }
}
