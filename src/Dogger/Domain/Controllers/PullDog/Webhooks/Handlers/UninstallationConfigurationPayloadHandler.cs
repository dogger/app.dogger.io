using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubPayloadInformation;
using MediatR;

namespace Dogger.Domain.Controllers.PullDog.Webhooks.Handlers
{
    public class UninstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;

        public string[] Events => new [] { "installation" };

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
            if (payload.Installation.Account == null)
                throw new InvalidOperationException("Account was not found.");

            var settings = await this.mediator.Send(
                new GetPullDogSettingsByGitHubPayloadInformationQuery(
                    payload.Installation.Id,
                    payload.Installation.Account.Id));
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
