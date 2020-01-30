using System.Threading.Tasks;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public interface IConfigurationCommitPayloadHandler
    {
        bool CanHandle(WebhookPayload payload);
        Task HandleAsync(WebhookPayload payload);
    }
}
