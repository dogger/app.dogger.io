using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Models;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
{
    public interface IConfigurationPayloadHandler
    {
        string Event { get; }

        bool CanHandle(WebhookPayload payload);

        Task HandleAsync(WebhookPayload payload);
    }
}
