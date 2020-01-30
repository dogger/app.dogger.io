using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using MediatR;

namespace Dogger.Domain.Events.ServerProvisioningStarted
{
    public class ServerProvisioningStartedEventHandler : IRequestHandler<ServerProvisioningStartedEvent>
    {
        private readonly IMediator mediator;

        public ServerProvisioningStartedEventHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(ServerProvisioningStartedEvent request, CancellationToken cancellationToken)
        {
            var instance = request.Instance;

            var pullRequest = instance.PullDogPullRequest;
            if (pullRequest == null)
                return Unit.Value;

            await this.mediator.Send(
                new UpsertPullRequestCommentCommand(
                    pullRequest,
                    "The test environment for this pull request is on its way :drum:\n\n_This typically takes a couple of minutes, depending on how long your images take to build, and how large the images are._"),
                cancellationToken);

            return Unit.Value;
        }
    }
}
