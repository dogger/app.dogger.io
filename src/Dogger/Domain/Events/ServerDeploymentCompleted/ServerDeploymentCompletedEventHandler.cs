using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
using Dogger.Domain.Queries.Clusters.GetConnectionDetails;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using MediatR;

namespace Dogger.Domain.Events.ServerDeploymentCompleted
{
    public class ServerDeploymentCompletedEventHandler : IRequestHandler<ServerDeploymentCompletedEvent>
    {
        private readonly IMediator mediator;

        public ServerDeploymentCompletedEventHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(ServerDeploymentCompletedEvent notification, CancellationToken cancellationToken)
        {
            var instance = await this.mediator.Send(
                new GetInstanceByNameQuery(notification.InstanceName),
                cancellationToken);
            if (instance == null)
                throw new InvalidOperationException("Instance was not found.");

            var pullRequest = instance.PullDogPullRequest;
            if (pullRequest == null)
                return Unit.Value;

            var connectionDetails = await this.mediator.Send(
                new GetConnectionDetailsQuery(notification.InstanceName),
                cancellationToken);
            if (connectionDetails == null)
                throw new InvalidOperationException("Could not fetch connection details for the given cluster.");

            var connectionDetailsPerPort = connectionDetails
                .Ports
                .Select(x => new
                {
                    x.Port,
                    x.Protocol,
                    HostName = connectionDetails.HostName ?? connectionDetails.IpAddress
                })
                .ToArray();

            var tableConnectionDetailsString = string
                .Join('\n', connectionDetailsPerPort
                    .Select(x => $"{x.HostName}|{x.Port}|{x.Protocol.ToString().ToUpperInvariant()}|[HTTP](http://{x.HostName}:{x.Port}) &nbsp; [HTTPS](httpS://{x.HostName}:{x.Port})")
                    .ToArray());
            var connectionDetailsString = connectionDetailsPerPort.Length == 0 ?
                "However, it doesn't seem to have any ports exposed. Make sure the Docker Compose YML contents expose a port." :
                $"Host name|Port|Protocol|Links\n-|-|-|-\n{tableConnectionDetailsString}";

            await this.mediator.Send(
                new UpsertPullRequestCommentCommand(
                    pullRequest,
                    $"The test environment for this pull request is ready! :tada:\n\n{connectionDetailsString}\n\n_It might take some time for your app to start up, so don't panick if the given connection details don't provide a response right away._"),
                cancellationToken);

            return Unit.Value;
        }
    }
}
