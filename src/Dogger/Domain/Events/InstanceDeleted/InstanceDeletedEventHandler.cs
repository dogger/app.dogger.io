using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.RemoveLabelFromGitHubPullRequest;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Queries.PullDog.GetConfigurationForPullRequest;
using MediatR;

namespace Dogger.Domain.Events.InstanceDeleted
{
    public class InstanceDeletedEventHandler : IRequestHandler<InstanceDeletedEvent>
    {
        private readonly IMediator mediator;

        public InstanceDeletedEventHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(InstanceDeletedEvent request, CancellationToken cancellationToken)
        {
            var pullRequest = request.DatabaseInstance.PullDogPullRequest;
            if (pullRequest == null)
                return Unit.Value;

            await this.mediator.Send(
                new UpsertPullRequestCommentCommand(
                    pullRequest,
                    "The test environment for this pull request has been destroyed :boom: This may have happened explicitly via a command, because the environment expired, or because the pull request was closed."),
                cancellationToken);

            var configuration = await mediator.Send(
                new GetConfigurationForPullRequestQuery(pullRequest),
                cancellationToken);
            if (configuration.Label != null)
            {
                await this.mediator.Send(
                    new RemoveLabelFromGitHubPullRequestCommand(
                        pullRequest,
                        configuration.Label),
                    cancellationToken);
            }

            return Unit.Value;
        }
    }
}
