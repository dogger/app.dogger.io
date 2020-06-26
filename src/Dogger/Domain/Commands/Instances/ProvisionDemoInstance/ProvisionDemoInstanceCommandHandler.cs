using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.Mediatr.Database;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.Instances.ProvisionDemoInstance
{
    public class ProvisionDemoInstanceCommandHandler : IRequestHandler<ProvisionDemoInstanceCommand, IProvisioningJob>, IDatabaseTransactionRequest
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;
        private readonly ISlackClient slackClient;

        private readonly DataContext dataContext;

        public ProvisionDemoInstanceCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            ISlackClient slackClient,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.slackClient = slackClient;
            this.dataContext = dataContext;
        }

        public async Task<IProvisioningJob> Handle(ProvisionDemoInstanceCommand request, CancellationToken cancellationToken)
        {
            await this.slackClient.PostAsync(new SlackMessage()
            {
                Text = "A demo instance is being provisioned.",
                Attachments = new List<SlackAttachment>()
                {
                    new SlackAttachment()
                    {
                        Fields = new List<SlackField>()
                        {
                            new SlackField()
                            {
                                Title = "User ID",
                                Value = request.AuthenticatedUserId?.ToString() ?? string.Empty,
                                Short = true
                            }
                        }
                    }
                }
            });

            var cluster = await mediator.Send(new EnsureClusterWithIdCommand(DataContext.DemoClusterId), cancellationToken);
            if (cluster.Instances.Count > 0)
            {
                var isDemoClusterOwnedByCurrentAuthenticatedUser = 
                    cluster.UserId != default && 
                    cluster.UserId == request.AuthenticatedUserId;
                if (isDemoClusterOwnedByCurrentAuthenticatedUser)
                    return this.provisioningService.GetCompletedJob();

                throw new DemoInstanceAlreadyProvisionedException();
            }

            var plan = await mediator.Send(new GetDemoPlanQuery(), cancellationToken);
            var instance = new Instance()
            {
                Name = "demo",
                Cluster = cluster,
                PlanId = plan.Id,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30)
            };

            cluster.UserId = request.AuthenticatedUserId;

            cluster.Instances.Add(instance);
            await this.dataContext.Instances.AddAsync(instance, cancellationToken);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return await this.provisioningService.ScheduleJobAsync(
                new ProvisionInstanceStageFlow(
                    plan.Id,
                    instance));
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }

}
