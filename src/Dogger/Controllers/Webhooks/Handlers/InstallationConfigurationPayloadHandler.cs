using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class InstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;

        public string Event => "installation_repositories";

        public InstallationConfigurationPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return 
                payload.Action == "added" || 
                payload.Action == "removed";
        }

        public async Task HandleAsync(WebhookPayload payload)
        {
            var settings = await this.mediator.Send(
                new GetPullDogSettingsByGitHubInstallationIdQuery(
                    payload.Installation.Id));
            if (settings == null)
                throw new InvalidOperationException("Could not find the given repository.");

            if (payload.RepositoriesRemoved != null)
            {
                foreach (var repository in payload.RepositoriesRemoved)
                {
                    await this.mediator.Send(new DeletePullDogRepositoryCommand(
                        repository.Id.ToString(CultureInfo.InvariantCulture)));
                }
            }

            if (payload.RepositoriesAdded != null)
            {
                foreach (var repository in payload.RepositoriesAdded)
                {
                    await this.mediator.Send(new EnsurePullDogRepositoryCommand(
                        settings,
                        repository.Id.ToString(CultureInfo.InvariantCulture)));
                }
            }
        }
    }
}
