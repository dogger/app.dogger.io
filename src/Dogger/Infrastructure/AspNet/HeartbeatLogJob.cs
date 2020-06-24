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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Dogger.Infrastructure.AspNet
{
    public class HeartbeatLogJob : TimedHostedService
    {
        public HeartbeatLogJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override TimeSpan Interval => TimeSpan.FromMinutes(1);

        protected override async Task OnTickAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var scopeLogger = serviceProvider.GetRequiredService<ILogger>();
            scopeLogger.Verbose("Scoped heartbeat.");

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var newLogger = LoggerFactory.BuildWebApplicationLogger(configuration);
            newLogger.Verbose("Scoped heartbeat.");
        }
    }
}
