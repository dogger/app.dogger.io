using System.Threading.Tasks;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IConfigurationPayloadHandler
    {
        string Event { get; }
        string Action { get; }

        Task HandleAsync(WebhookPayload payload);
    }
}
