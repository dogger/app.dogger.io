using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Controllers.PullDog.Webhooks.Models;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.Slack;
using Flurl.Util;
using MediatR;
using Serilog;
using Slack.Webhooks;

namespace Dogger.Controllers.PullDog.Webhooks.Handlers
{
    public class InstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly ILogger logger;

        public string Event => "installation_repositories";

        public InstallationConfigurationPayloadHandler(
            IMediator mediator,
            IGitHubClientFactory gitHubClientFactory,
            ILogger logger)
        {
            this.mediator = mediator;
            this.gitHubClientFactory = gitHubClientFactory;
            this.logger = logger;
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
            {
                this.logger.Error("No settings error occured - will log more details.");

                var client = await this.gitHubClientFactory.CreateInstallationClientAsync(payload.Installation.Id);
                var currentUser = await client.User.Current();
                this.logger.Error("An error occured with user {@User}.", currentUser);

                throw new InvalidOperationException($"Could not find Pull Dog settings for an installation ID of {payload.Installation.Id}.");
            }

            if (payload.RepositoriesRemoved != null)
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

            if (payload.RepositoriesAdded != null)
            {
                await this.mediator.Send(new SendSlackMessageCommand("Pull Dog repositories have been installed :sunglasses:")
                {
                    Fields = payload
                        .RepositoriesAdded
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
                        payload.RepositoriesAdded
                            .Select(x => x.Id)
                            .ToArray()));
            }
        }
    }
}
