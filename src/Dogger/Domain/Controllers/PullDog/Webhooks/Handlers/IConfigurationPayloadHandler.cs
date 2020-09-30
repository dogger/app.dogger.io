using System.Threading.Tasks;
using Dogger.Domain.Controllers.PullDog.Webhooks.Models;

namespace Dogger.Domain.Controllers.PullDog.Webhooks.Handlers
{
    public interface IConfigurationPayloadHandler
    {
        string[] Events { get; }

        bool CanHandle(WebhookPayload payload);

        Task HandleAsync(WebhookPayload payload);
    }
}
