using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
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

            var settings = repository.PullDogSettings;
            if (settings?.GitHubInstallationId == null)
                throw new InvalidOperationException("Could not fetch the GitHub installation ID.");

            if(!long.TryParse(repository.Handle, out var repositoryId))
                throw new InvalidOperationException("Invalid repository handle.");

            if (!int.TryParse(request.PullRequest.Handle, out var pullRequestNumber))
                throw new InvalidOperationException("Invalid pull request handle.");

            var client = await this.gitHubClientFactory.CreateInstallationClientAsync(
                settings.GitHubInstallationId.Value);

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

            var requestContent = $"\\*Ruff\\* :dog: {request.Content}\n\nReact on this comment to leave anonymous feedback.\n- :+1: to say _good dog_ :meat_on_bone:\n- :-1: to say _bad dog_ :bone:\n\n{RenderSpoiler("Commands", "- `@pull-dog go fetch` to reprovision or provision the server.\n- `@pull-dog get lost` to delete the provisioned server.")}";
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

        private static string RenderSpoiler(string title, string content)
        {
            return $"<details>\n<summary>{title}</summary>\n\n{content}\n</details>";
        }
    }
}
