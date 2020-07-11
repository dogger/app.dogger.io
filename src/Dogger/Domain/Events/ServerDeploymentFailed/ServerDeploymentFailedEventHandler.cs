using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.Instances.SetInstanceExpiry;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Helpers;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using MediatR;

namespace Dogger.Domain.Events.ServerDeploymentFailed
{
    public class ServerDeploymentFailedEventHandler : IRequestHandler<ServerDeploymentFailedEvent>
    {
        private readonly IMediator mediator;

        public ServerDeploymentFailedEventHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(ServerDeploymentFailedEvent request, CancellationToken cancellationToken)
        {
            var instance = await this.mediator.Send(
                new GetInstanceByNameQuery(request.InstanceName),
                cancellationToken);
            if (instance == null)
                return Unit.Value;

            await this.mediator.Send(
                new SetInstanceExpiryCommand(
                    request.InstanceName,
                    instance
                        .CreatedAtUtc
                        .AddMinutes(55)),
                cancellationToken);

            var pullRequest = instance.PullDogPullRequest;
            if (pullRequest != null)
            {
                await this.mediator.Send(
                    new UpsertPullRequestCommentCommand(
                        pullRequest,
                        $"Could not run `docker-compose up` on the server.\n\n**Response from Docker**\n>{request.Reason.Replace("\n\n", "\n", StringComparison.InvariantCulture)}\n\n{GitHubCommentHelper.RenderSpoiler("Server file list dump", $"```\n{request.FileListDump}\n```")}"),
                    cancellationToken);
            }

            return Unit.Value;
        }
    }
}
