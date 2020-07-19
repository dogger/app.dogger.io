using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Docker.Yml;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment
{
    public class ProvisionPullDogEnvironmentCommandHandler : IRequestHandler<ProvisionPullDogEnvironmentCommand>
    {
        private readonly IMediator mediator;
        private readonly IProvisioningService provisioningService;
        private readonly ISlackClient slackClient;
        private readonly IPullDogFileCollectorFactory pullDogFileCollectorFactory;

        public ProvisionPullDogEnvironmentCommandHandler(
            IMediator mediator,
            IProvisioningService provisioningService,
            ISlackClient slackClient,
            IPullDogFileCollectorFactory pullDogFileCollectorFactory)
        {
            this.mediator = mediator;
            this.provisioningService = provisioningService;
            this.slackClient = slackClient;
            this.pullDogFileCollectorFactory = pullDogFileCollectorFactory;
        }

        public async Task<Unit> Handle(ProvisionPullDogEnvironmentCommand request, CancellationToken cancellationToken)
        {
            var repository = request.Repository;

            if (repository.GitHubInstallationId == null)
                throw new InvalidOperationException("The GitHub installation ID could not be determined.");

            var pullRequest = await this.mediator.Send(
                new EnsurePullDogPullRequestCommand(
                    repository,
                    request.PullRequestHandle),
                cancellationToken);
            if (request.ConfigurationOverride != null)
            {
                await this.mediator.Send(
                    new OverrideConfigurationForPullRequestCommand(
                        pullRequest.Id,
                        request.ConfigurationOverride),
                    cancellationToken);

                pullRequest.ConfigurationOverride = request.ConfigurationOverride;
            }

            var configuration = await this.mediator.Send(
                new GetConfigurationForPullRequestQuery(pullRequest),
                cancellationToken);
            if (configuration == null)
                return Unit.Value;

            if (configuration.IsLazy)
            {
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        "I am running in lazy mode (as per your `pull-dog.json` configuration file), so I won't start provisioning a test environment for this pull request until I hear from your build server :zzz: Give it a few minutes, and check back."),
                    cancellationToken);
                return Unit.Value;
            }

            var client = await pullDogFileCollectorFactory.CreateAsync(pullRequest);
            var files = await client.GetRepositoryFilesFromConfiguration(configuration);
            if (files == null)
            {
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        "I wasn't able to find any Docker Compose files in your repository at any of the given paths in the `pull-dog.json` configuration file, or the default `docker-compose.yml` file :weary: Make sure the given paths are correct."),
                    cancellationToken);
                return Unit.Value;
            }

            try
            {
                var settings = repository.PullDogSettings;
                await ReportProvisioningToSlackAsync(request, settings);

                var instance = await this.mediator.Send(
                    new EnsurePullDogDatabaseInstanceCommand(
                        pullRequest,
                        configuration),
                    cancellationToken);

                var flowsToUse = new List<IProvisioningStateFlow>();
                if (!instance.IsProvisioned)
                {
                    flowsToUse.Add(new ProvisionInstanceStateFlow(
                        settings.PlanId,
                        instance));
                }

                flowsToUse.Add(new DeployToClusterStateFlow(
                    instance.Name,
                    configuration.DockerComposeYmlFilePaths)
                {
                    Files = files.Select(file => new InstanceDockerFile(
                        file.Path,
                        file.Contents)),
                    BuildArguments = configuration.BuildArguments
                });

                await provisioningService.ScheduleJobAsync(
                    new AggregateProvisioningStateFlow(flowsToUse.ToArray()));
            }
            catch (PullDogPoolSizeExceededException ex)
            {
                var offendingPullRequestListText = string.Join('\n', ex
                    .OffendingPullRequests
                    .Select(x => $"- {x.DirectPullRequestCommentReference}"));
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        $"It looks like you are currently using the maximum amount of concurrent test environments :tired_face:\n\nYou can [upgrade your plan](https://dogger.io/dashboard/pull-dog) to increase that limit, and our plans are quite cheap.\n\nThe following pull requests are using environments from your pool as of writing this comment (they may have since expired):\n{offendingPullRequestListText}"),
                    cancellationToken);
            }
            catch (PullDogDemoInstanceAlreadyProvisionedException ex)
            {
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        $"I tried to provision a test environment for your pull request, but there aren't enough free-plan servers available :tired_face:\n\nWe'll keep trying every time you open a new pull request in the future, but if you want to make sure to always have a test environment available, you need to [upgrade your plan](https://dogger.io/dashboard/pull-dog) to a paid plan.\n\nThe latest pull request that is currently using a demo server is: {ex.OffendingPullRequests.Last().IndirectPullRequestCommentReference}"),
                    cancellationToken);
            }
            catch (DockerComposeSyntaxErrorException)
            {
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        $"Your Docker Compose YML file was not valid. Try running `docker-compose up` on it yourself locally, to find any potential issues."),
                    cancellationToken);
            }

            return Unit.Value;
        }

        private async Task ReportProvisioningToSlackAsync(ProvisionPullDogEnvironmentCommand request, PullDogSettings settings)
        {
            await this.slackClient.PostAsync(
                new SlackMessage()
                {
                    Text = "A Pull Dog instance is being provisioned.",
                    Attachments = new List<SlackAttachment>()
                    {
                        new SlackAttachment()
                        {
                            Fields = new List<SlackField>()
                            {
                                new SlackField()
                                {
                                    Title = "Repository handle",
                                    Value = request.Repository.Handle,
                                    Short = true
                                },
                                new SlackField()
                                {
                                    Title = "Pull request handle",
                                    Value = request.PullRequestHandle,
                                    Short = true
                                },
                                new SlackField()
                                {
                                    Title = "Installation ID",
                                    Value = request.Repository.GitHubInstallationId.ToString(),
                                    Short = true
                                }
                            }
                        }
                    }
                });
        }
    }

}
