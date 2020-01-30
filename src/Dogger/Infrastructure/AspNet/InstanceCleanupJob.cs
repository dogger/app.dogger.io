using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Infrastructure.AspNet
{
    public class InstanceCleanupJob : TimedHostedService
    {
        public InstanceCleanupJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override TimeSpan Interval => TimeSpan.FromMinutes(3);

        protected override async Task OnTickAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var mediator = serviceProvider.GetRequiredService<IMediator>();

            await CleanUpDemoClusterAsync(mediator, cancellationToken);
        }

        private static async Task CleanUpDemoClusterAsync(IMediator mediator, CancellationToken cancellationToken)
        {
            var demoLightsailInstance = await mediator.Send(new GetLightsailInstanceByNameQuery("demo"), cancellationToken);

            var demoCluster = await mediator.Send(new EnsureClusterWithIdCommand(DataContext.DemoClusterId), cancellationToken);
            var demoDatabaseInstance = demoCluster.Instances.FirstOrDefault();
            if (demoDatabaseInstance == null && demoLightsailInstance == null)
                return;

            if (demoLightsailInstance != null && DateTime.UtcNow - demoLightsailInstance.CreatedAt < TimeSpan.FromMinutes(30))
                return;

            await mediator.Send(
                new DeleteInstanceByNameCommand("demo"),
                cancellationToken);
        }
    }
}
