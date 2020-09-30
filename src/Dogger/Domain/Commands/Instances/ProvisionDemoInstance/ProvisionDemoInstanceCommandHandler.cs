using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Models.Builders;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.Mediatr.Database;
using Dogger.Infrastructure.Slack;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.Instances.ProvisionDemoInstance
{
    public class ProvisionDemoInstanceCommandHandler : IRequestHandler<ProvisionDemoInstanceCommand, IProvisioningJob>, IDatabaseTransactionRequest
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public ProvisionDemoInstanceCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<IProvisioningJob> Handle(ProvisionDemoInstanceCommand request, CancellationToken cancellationToken)
        {
            await this.mediator.Send(
                new SendSlackMessageCommand("A demo instance is being provisioned :sunglasses:")
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
                },
                cancellationToken);

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
            var instance = new InstanceBuilder()
                .WithName("demo")
                .WithCluster(cluster)
                .WithProvisionedStatus(false)
                .WithPlanId(plan.Id)
                .WithExpiredDate(DateTime.UtcNow.AddMinutes(30))
                .Build();

            cluster.UserId = request.AuthenticatedUserId;

            cluster.Instances.Add(instance);
            await this.dataContext.Instances.AddAsync(instance, cancellationToken);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return await this.provisioningService.ScheduleJobAsync(
                cluster.Id.ToString(),
                new ProvisionInstanceStateFlow(
                    plan.Id,
                    instance));
        }

        public IsolationLevel? TransactionIsolationLevel => default;
    }

}
