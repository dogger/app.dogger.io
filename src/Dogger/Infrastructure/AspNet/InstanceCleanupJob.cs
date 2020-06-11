using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Instances.GetExpiredInstances;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Infrastructure.AspNet
{
    public class InstanceCleanupJob : TimedHostedService
    {
        public InstanceCleanupJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

        protected override async Task OnTickAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var mediator = serviceProvider.GetRequiredService<IMediator>();

            await CleanUpDemoClusterAsync(mediator, cancellationToken);
        }

        private static async Task CleanUpDemoClusterAsync(
            IMediator mediator, 
            CancellationToken cancellationToken)
        {
            var expiredInstances = await mediator.Send(new GetExpiredInstancesQuery(), cancellationToken);
            foreach (var expiredInstance in expiredInstances)
            {
                await mediator.Send(
                    new DeleteInstanceByNameCommand(expiredInstance.Name),
                    cancellationToken);
            }
        }
    }
}
