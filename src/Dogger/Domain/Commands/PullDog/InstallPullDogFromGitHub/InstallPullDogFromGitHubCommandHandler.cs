using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.Slack;
using MediatR;
using Octokit;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub
{
    public class InstallPullDogFromGitHubCommandHandler : IRequestHandler<InstallPullDogFromGitHubCommand>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public InstallPullDogFromGitHubCommandHandler(
            IGitHubClientFactory gitHubClientFactory,
            IMediator mediator,
            DataContext dataContext)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(InstallPullDogFromGitHubCommand request, CancellationToken cancellationToken)
        {
            var client = await this.gitHubClientFactory.CreateInstallationInitiatorClientAsync(request.Code);
            var currentUser = await client.User.Current();
            var emails = await client.User.Email.GetAll();

            await this.mediator.Send(
                new SendSlackMessageCommand("Pull Dog was installed by a user :sunglasses:")
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
                },
                cancellationToken);

            var validatedEmailsOrderedByImportance = emails
                .Where(x => x.Verified)
                .OrderByDescending(x => 
                    (x.Primary ? 2 : 0) +
                    (x.Visibility == EmailVisibility.Public ? 1 : 0))
                .Select(x => x.Email)
                .ToList();
            if (validatedEmailsOrderedByImportance.Count == 0)
                throw new InvalidOperationException("The user does not have any validated emails.");

            var auth0User = await this.mediator.Send(
                new GetAuth0UserFromGitHubUserIdQuery(currentUser.Id), 
                cancellationToken);
            if (auth0User?.EmailVerified == true)
                validatedEmailsOrderedByImportance.Insert(0, auth0User.Email);

            var user = await this.mediator.Send(
                new InstallPullDogFromEmailsCommand(validatedEmailsOrderedByImportance.ToArray())
                {
                    Plan = request.Plan
                },
                cancellationToken);
            if (user.PullDogSettings == null)
                throw new InvalidOperationException("Pull Dog settings were not installed properly on user.");

            var installationClient = await this.gitHubClientFactory.CreateInstallationClientAsync(request.InstallationId);
            var installedRepositories = await installationClient.GitHubApps.Installation.GetAllRepositoriesForCurrent();
            if (installedRepositories.TotalCount > installedRepositories.Repositories.Count)
                throw new InvalidOperationException("Did not fetch all repositories.");

            await this.mediator.Send(
                new AddPullDogToGitHubRepositoriesCommand(
                    request.InstallationId,
                    user.PullDogSettings,
                    installedRepositories
                        .Repositories
                        .Select(x => x.Id)
                        .ToArray()),
                cancellationToken);

            await dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
