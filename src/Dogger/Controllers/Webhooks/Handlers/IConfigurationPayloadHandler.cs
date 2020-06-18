using System.Threading.Tasks;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IConfigurationPayloadHandler
    {
        string Event { get; }

        bool CanHandle(WebhookPayload payload);

        Task HandleAsync(WebhookPayload payload);
    }
}
