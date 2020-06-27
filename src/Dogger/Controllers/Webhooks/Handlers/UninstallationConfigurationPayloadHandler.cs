using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class UninstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event => "installation";

        public UninstallationConfigurationPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return payload.Action == "deleted";
        }

        public async Task HandleAsync(WebhookPayload payload)
        {
            var settings = await this.mediator.Send(
                new GetPullDogSettingsByGitHubInstallationIdQuery(payload.Installation.Id));
            if (settings == null)
                return;

            foreach (var repository in settings.Repositories.ToArray())
            {
                if (repository.GitHubInstallationId != payload.Installation.Id)
                    continue;

                await this.mediator.Send(
                    new DeletePullDogRepositoryCommand(repository.Handle));
            }
        }
    }
}
