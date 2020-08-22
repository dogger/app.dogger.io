using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterForUser;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.Ioc;
using Dogger.Infrastructure.Slack;
using MediatR;
using Slack.Webhooks;

namespace Dogger.Domain.Commands.Instances.ProvisionInstanceForUser
{
    public class ProvisionInstanceForUserCommandHandler : IRequestHandler<ProvisionInstanceForUserCommand, IProvisioningJob>
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public ProvisionInstanceForUserCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<IProvisioningJob> Handle(
            ProvisionInstanceForUserCommand request, 
            CancellationToken cancellationToken)
        {
            await this.mediator.Send(
                new SendSlackMessageCommand("A paid instance is being provisioned :sunglasses:")
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
                },
                cancellationToken);

            var cluster = await mediator.Send(
                new EnsureClusterForUserCommand(request.User.Id), 
                cancellationToken);

            var instance = new Instance()
            {
                Name = $"{request.User.Id}_{Guid.NewGuid()}",
                Cluster = cluster,
                IsProvisioned = false,
                PlanId = request.Plan.Id
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
