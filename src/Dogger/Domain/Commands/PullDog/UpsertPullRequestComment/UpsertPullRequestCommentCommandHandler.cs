﻿using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Helpers;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure;
using Dogger.Infrastructure.GitHub;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.UpsertPullRequestComment
{
    public class UpsertPullRequestCommentCommandHandler : IRequestHandler<UpsertPullRequestCommentCommand>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly IMediator mediator;

        public UpsertPullRequestCommentCommandHandler(
            IGitHubClientFactory gitHubClientFactory,
            IMediator mediator)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(UpsertPullRequestCommentCommand request, CancellationToken cancellationToken)
        {
            var repository = request.PullRequest.PullDogRepository;

            if (repository?.GitHubInstallationId == null)
                throw new InvalidOperationException("Could not fetch the GitHub installation ID.");

            if(!long.TryParse(repository.Handle, out var repositoryId))
                throw new InvalidOperationException("Invalid repository handle.");

            if (!int.TryParse(request.PullRequest.Handle, out var pullRequestNumber))
                throw new InvalidOperationException("Invalid pull request handle.");

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(
                repository.GitHubInstallationId.Value);
            if (client == null)
                return Unit.Value;

            var pullRequest = await client.PullRequest.Get(
                repositoryId,
                pullRequestNumber);

            var headRepository = pullRequest.Base.Repository;

            var comments = await client
                .Issue
                .Comment
                .GetAllForIssue(
                    headRepository.Id, 
                    pullRequest.Number);

            var configuration = await mediator.Send(
                new GetConfigurationForPullRequestQuery(request.PullRequest),
                cancellationToken);
            var shouldSkipUpsert = configuration?.ConversationMode == ConversationMode.MultipleComments;

            var existingBotComment = !shouldSkipUpsert ?
                comments.FirstOrDefault(x => 
                    x.User.Id == 64123634 ||
                    x.User.Id == 64746321) :
                null;

            var requestContent = $"\\*Ruff\\* :dog: {request.Content}\n\n{RenderWhatIsThisSection()}{RenderCommandsSection()}{RenderDebugSection(configuration, request)}";
            if (existingBotComment == null)
            {
                await client
                    .Issue
                    .Comment
                    .Create(
                        headRepository.Id, 
                        pullRequest.Number, 
                        requestContent);
            }
            else
            {
                await client
                    .Issue
                    .Comment
                    .Update(
                        headRepository.Id, 
                        existingBotComment.Id,
                        requestContent);
            }

            return Unit.Value;
        }

        private static string RenderDebugSection(
            ConfigurationFile? configuration,
            UpsertPullRequestCommentCommand request)
        {
            var options = JsonFactory.GetOptions();
            options.WriteIndented = true;

            var configurationJson = JsonSerializer.Serialize(
                configuration ?? new ConfigurationFile(), 
                options);

            return GitHubCommentHelper.RenderSpoiler("Troubleshooting", $"Need help? Don't hesitate to <a href=\"https://github.com/dogger/app.dogger.io/issues/new\">file an issue</a> in our repository\n\n**Configuration**\n{GitHubCommentHelper.RenderCodeBlock("json", configurationJson)}\n\n**Trace ID**\n{request.TraceId ?? "No trace ID"}");
        }

        private static string RenderCommandsSection()
        {
            return GitHubCommentHelper.RenderSpoiler("Commands", "- `@pull-dog up` to reprovision or provision the server.\n- `@pull-dog down` to delete the provisioned server.");
        }

        private static string RenderWhatIsThisSection()
        {
            return GitHubCommentHelper.RenderSpoiler("What is this?", "<a href=\"https://dogger.io\" target=\"_blank\">Pull Dog</a> is a GitHub app that makes test environments for your pull requests using Docker, from a `docker-compose.yml` file you specify. It takes 19 seconds to set up (we counted!) and there's a free plan available.\n\nVisit <a href=\"https://dogger.io\" target=\"_blank\">our website</a> to learn more.");
        }
    }
}
