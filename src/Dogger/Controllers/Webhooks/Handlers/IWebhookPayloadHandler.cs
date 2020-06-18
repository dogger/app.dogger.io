using System.Threading.Tasks;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IWebhookPayloadHandler
    {
        string Event { get; }
        string Action { get; }

        Task HandleAsync(WebhookPayloadContext context);
    }
}
