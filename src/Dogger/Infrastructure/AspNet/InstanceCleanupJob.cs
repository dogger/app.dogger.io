using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
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

            await CleanUpExpiredInstancesAsync(mediator, cancellationToken);
        }

        private static async Task CleanUpExpiredInstancesAsync(
            IMediator mediator, 
            CancellationToken cancellationToken)
        {
            var expiredInstances = await mediator.Send(new GetExpiredInstancesQuery(), cancellationToken);
            foreach (var expiredInstance in expiredInstances)
            {
                await mediator.Send(
                    new DeleteInstanceByNameCommand(
                        expiredInstance.Name,
                        InitiatorType.System),
                    cancellationToken);
            }
        }
    }
}
