using System.Threading.Tasks;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IWebhookPayloadHandler
    {
        bool CanHandle(WebhookPayload payload);

        Task HandleAsync(WebhookPayloadContext context);
    }
}
