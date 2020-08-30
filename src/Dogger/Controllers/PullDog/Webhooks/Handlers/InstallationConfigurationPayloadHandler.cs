using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubPayloadInformation;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.Slack;
using MediatR;
using Serilog;
using Slack.Webhooks;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
{
    public class InstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;

        public string[] Events => new []
        {
            "installation_repositories", 
            "installation"
        };

        public InstallationConfigurationPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return 
                payload.Action == "added" || 
                payload.Action == "created" ||
                payload.Action == "removed";
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

            if (payload.RepositoriesRemoved?.Length > 0)
            {
                await this.mediator.Send(new SendSlackMessageCommand("Pull Dog repositories have been uninstalled :frowning:")
                {
                    Fields = payload
                        .RepositoriesRemoved
                        .Select(x => new SlackField
                        {
                            Short = true,
                            Title = x.FullName,
                            Value = x.Id.ToString(CultureInfo.InvariantCulture)
                        })
                });

                foreach (var repository in payload.RepositoriesRemoved)
                {
                    await this.mediator.Send(new DeletePullDogRepositoryCommand(
                        repository.Id.ToString(CultureInfo.InvariantCulture)));
                }
            }

            var repositoriesAdded = GetAddedRepositories(payload);
            if (repositoriesAdded?.Length > 0)
            {
                await this.mediator.Send(new SendSlackMessageCommand("Pull Dog repositories have been installed :sunglasses:")
                {
                    Fields = repositoriesAdded
                        .Select(x => new SlackField
                        {
                            Short = true,
                            Title = x.FullName,
                            Value = x.Id.ToString(CultureInfo.InvariantCulture)
                        })
                });

                await this.mediator.Send(
                    new AddPullDogToGitHubRepositoriesCommand(
                        payload.Installation.Id,
                        settings,
                        repositoriesAdded
                            .Select(x => x.Id)
                            .ToArray()));
            }
        }

        private static InstallationRepositoryReferencePayload[]? GetAddedRepositories(WebhookPayload payload)
        {
            return 
                payload.RepositoriesAdded ??
                payload.Repositories;
        }
    }
}
