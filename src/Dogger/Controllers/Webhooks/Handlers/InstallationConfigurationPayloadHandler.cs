using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
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
                throw new InvalidOperationException($"Could not find Pull Dog settings for an installation ID of {payload.Installation.Id}.");

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
                await this.mediator.Send(
                    new AddPullDogToGitHubRepositoriesCommand(
                        payload.Installation.Id,
                        settings,
                        payload.RepositoriesAdded
                            .Select(x => x.Id)
                            .ToArray()));
            }
        }
    }
}
