using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using MediatR;

namespace Dogger.Controllers.Webhooks.Handlers
{
    public class InstallationConfigurationPayloadHandler : IConfigurationPayloadHandler
    {
        private readonly IMediator mediator;

        private const string masterReference = "refs/heads/master";

        public InstallationConfigurationPayloadHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public bool CanHandle(WebhookPayload payload)
        {
            return IsInstallPayload(payload);
        }

        private static bool IsInstallPayload(WebhookPayload payload)
        {
            return
                IsMasterCommitPayload(payload) &&
                payload.Commits.Any(x =>
                   x.Added.Contains("pull-dog.json") ||
                   x.Modified.Contains("pull-dog.json"));
        }

        private static bool IsMasterCommitPayload(WebhookPayload payload)
        {
            return
                IsCommitPayload(payload) &&
                payload.Reference == masterReference;
        }

        private static bool IsCommitPayload(WebhookPayload payload)
        {
            return payload.Pusher != null && payload.Commits != null;
        }

        public async Task HandleAsync(WebhookPayload payload)
        {
            var settings = await this.mediator.Send(
                new GetPullDogSettingsByGitHubInstallationIdQuery(
                    payload.Installation.Id));
            if (settings == null)
                throw new InvalidOperationException("Could not find the given repository.");

            var repositoryHandle =
                payload.Repository?.Id.ToString(CultureInfo.InvariantCulture) ??
                throw new InvalidOperationException("Repository ID not found");
            await this.mediator.Send(new EnsurePullDogRepositoryCommand(
                settings,
                repositoryHandle));
        }
    }
}
