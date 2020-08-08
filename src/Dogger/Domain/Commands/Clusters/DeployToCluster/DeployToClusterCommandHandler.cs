using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Clusters.GetClusterById;
using Dogger.Domain.Queries.Clusters.GetClusterForUser;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.Clusters.DeployToCluster
{
    public class DeployToClusterCommandHandler : IRequestHandler<DeployToClusterCommand, IProvisioningJob>
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;
        private readonly ISlackClient? slackClient;

        public DeployToClusterCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            IOptionalService<ISlackClient> slackClient)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.slackClient = slackClient.Value;
        }

        public async Task<IProvisioningJob> Handle(DeployToClusterCommand request, CancellationToken cancellationToken)
        {
            Cluster? cluster;

            if (request.ClusterId != null)
            {
                cluster = await this.mediator.Send(
                    new GetClusterByIdQuery(request.ClusterId.Value),
                    cancellationToken);

                if (request.ClusterId == DataContext.DemoClusterId && this.slackClient != null)
                {
                    await this.slackClient.PostAsync(new SlackMessage()
                    {
                        Text = $"A demo instance is being requested.",
                        Attachments = new List<SlackAttachment>()
                        {
                            new SlackAttachment()
                            {
                                Fields = new List<SlackField>()
                                {
                                    new SlackField()
                                    {
                                        Title = "User ID",
                                        Value = request.UserId?.ToString() ?? string.Empty,
                                        Short = true
                                    }
                                }
                            }
                        }
                    });
                }
            }
            else
            {
                if (request.UserId == null)
                    throw new InvalidOperationException("Either ClusterId or UserId must be specified.");

                cluster = await this.mediator.Send(new GetClusterForUserQuery(request.UserId.Value)
                {
                    ClusterId = request.ClusterId
                }, cancellationToken);
            }

            if (cluster == null)
                throw new ClusterNotFoundException();

            if (cluster.UserId != request.UserId)
                throw new NotAuthorizedToAccessClusterException();

            var instance = cluster.Instances.Single();

            return await this.provisioningService.ScheduleJobAsync(new DeployToClusterStateFlow(
                instance.Name,
                request.DockerComposeYmlFilePaths)
            {
                Files = request
                    .Files
                    ?.Select(x => new InstanceDockerFile(
                        x.Path,
                        x.Contents)),
                Authentication = request.Authentication
            });
        }
    }

}
