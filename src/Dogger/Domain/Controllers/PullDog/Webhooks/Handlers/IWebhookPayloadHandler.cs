using System.Threading.Tasks;
using Dogger.Domain.Controllers.PullDog.Webhooks.Models;

namespace Dogger.Domain.Controllers.PullDog.Webhooks.Handlers
{
    public interface IWebhookPayloadHandler
    {
        string Event { get; }

        bool CanHandle(WebhookPayload payload);
        Task HandleAsync(WebhookPayloadContext context);
    }
}
