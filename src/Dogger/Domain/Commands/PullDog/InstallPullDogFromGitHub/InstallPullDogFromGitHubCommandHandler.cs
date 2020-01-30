using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Auth0.CreateAuth0User;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Infrastructure.Encryption;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Octokit;
using Slack.Webhooks;
using User = Auth0.ManagementApi.Models.User;

namespace Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub
{
    public class InstallPullDogFromGitHubCommandHandler : IRequestHandler<InstallPullDogFromGitHubCommand>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly IMediator mediator;
        private readonly ISlackClient slackClient;
        private readonly IAesEncryptionHelper aesEncryptionHelper;

        private readonly DataContext dataContext;

        public InstallPullDogFromGitHubCommandHandler(
            IGitHubClientFactory gitHubClientFactory,
            IMediator mediator,
            ISlackClient slackClient,
            IAesEncryptionHelper aesEncryptionHelper,
            DataContext dataContext)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.mediator = mediator;
            this.slackClient = slackClient;
            this.aesEncryptionHelper = aesEncryptionHelper;
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(InstallPullDogFromGitHubCommand request, CancellationToken cancellationToken)
        {
            var client = await this.gitHubClientFactory.CreateInstallationInitiatorClientAsync(request.Code);
            var currentUser = await client.User.Current();
            var emails = await client.User.Email.GetAll();

            await this.slackClient.PostAsync(new SlackMessage()
            {
                Text = $"Pull Dog was installed by a user.",
                Attachments = new List<SlackAttachment>()
                {
                    new SlackAttachment()
                    {
                        Fields = new List<SlackField>()
                        {
                            new SlackField()
                            {
                                Title = "GitHub user login",
                                Value = currentUser.Login,
                                Short = true
                            },
                            new SlackField()
                            {
                                Title = "GitHub user ID",
                                Value = currentUser.Id.ToString(CultureInfo.InvariantCulture),
                                Short = true
                            }
                        }
                    }
                }
            });

            var validatedEmails = emails
                .Where(x => x.Verified)
                .ToArray();
            if (validatedEmails.Length == 0)
                throw new InvalidOperationException("The user does not have any validated emails.");

            var auth0User = await EnsureAuth0UserForGitHubUserAsync(
                currentUser.Id,
                validatedEmails,
                cancellationToken);

            var preferredEmail =
                validatedEmails.SingleOrDefault(x => x.Primary) ??
                validatedEmails.First();
            var user = await this.mediator.Send(
                new EnsureUserForIdentityCommand(
                    auth0User.UserId,
                    preferredEmail.Email),
                cancellationToken);
            if (user.PullDogSettings != null)
            {
                user.PullDogSettings.GitHubInstallationId = request.InstallationId;
            }
            else
            {
                var plan = await this.mediator.Send(
                    new GetDemoPlanQuery(),
                    cancellationToken);
                user.PullDogSettings = new PullDogSettings()
                {
                    GitHubInstallationId = request.InstallationId,
                    PlanId = plan.Id,
                    PoolSize = 0,
                    EncryptedApiKey = await this.aesEncryptionHelper.EncryptAsync(
                        Guid.NewGuid().ToString())
                };
            }

            await dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }

        private async Task<User> EnsureAuth0UserForGitHubUserAsync(int userId, EmailAddress[] userEmails, CancellationToken cancellationToken)
        {
            var emailStrings = userEmails
                .Select(x => x.Email)
                .ToArray();
            return
                await this.mediator.Send(new GetAuth0UserFromGitHubUserIdQuery(userId), cancellationToken) ??
                await this.mediator.Send(new GetAuth0UserFromEmailsQuery(emailStrings), cancellationToken) ??
                await this.mediator.Send(new CreateAuth0UserCommand(emailStrings), cancellationToken);
        }
    }
}
