using System.Threading.Tasks;
using Dogger.Controllers.Webhooks.Models;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IWebhookPayloadHandler
    {
        string Event { get; }

        bool CanHandle(WebhookPayload payload);
        Task HandleAsync(WebhookPayloadContext context);
    }
}
