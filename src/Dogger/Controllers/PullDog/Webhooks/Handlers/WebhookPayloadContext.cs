using System.Diagnostics.CodeAnalysis;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Models;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
{
    [ExcludeFromCodeCoverage]
    public class WebhookPayloadContext
    {
        public WebhookPayload Payload { get; }
        public PullDogSettings Settings { get; }
        public PullDogRepository Repository { get; }
        public PullDogPullRequest PullRequest { get; }

        public WebhookPayloadContext(
            WebhookPayload payload,
            PullDogSettings settings,
            PullDogRepository repository,
            PullDogPullRequest pullRequest)
        {
            this.Payload = payload;
            this.Settings = settings;
            this.Repository = repository;
            this.PullRequest = pullRequest;
        }
    }
}
