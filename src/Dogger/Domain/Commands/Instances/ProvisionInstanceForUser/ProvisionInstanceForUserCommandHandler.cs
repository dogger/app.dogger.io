using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.Instances.ProvisionInstanceForUser
{
    public class ProvisionInstanceForUserCommandHandler : IRequestHandler<ProvisionInstanceForUserCommand, IProvisioningJob>
    {
        private readonly IProvisioningService provisioningService;
        private readonly ISlackClient slackClient;
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public ProvisionInstanceForUserCommandHandler(
            IProvisioningService provisioningService,
            ISlackClient slackClient,
            IMediator mediator,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.slackClient = slackClient;
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<IProvisioningJob> Handle(
            ProvisionInstanceForUserCommand request, 
            CancellationToken cancellationToken)
        {
            await this.slackClient.PostAsync(new SlackMessage()
            {
                Text = "A paid instance is being provisioned.",
                Attachments = new List<SlackAttachment>()
                {
                    new SlackAttachment()
                    {
                        Fields = new List<SlackField>()
                        {
                            new SlackField()
                            {
                                Title = "User ID",
                                Value = request.User.Id.ToString(),
                                Short = true
                            },
                            new SlackField()
                            {
                                Title = "Plan",
                                Value = request.Plan.Id,
                                Short = true
                            }
                        }
                    }
                }
            });

            var cluster = await mediator.Send(new EnsureClusterForUserCommand(request.User.Id), cancellationToken);

            var instance = new Instance()
            {
                Name = $"{request.User.Id}_{Guid.NewGuid()}",
                Cluster = cluster,
                IsProvisioned = false,
                PlanId = request.Plan.Id,
                Type = InstanceType.DockerCompose
            };

            cluster.Instances.Add(instance);
            await this.dataContext.Instances.AddAsync(instance, cancellationToken);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return await this.provisioningService.ScheduleJobAsync(
                new ProvisionInstanceStateFlow(
                    request.Plan.Id,
                    instance)
                {
                    UserId = request.User.Id
                });
        }
    }
}
