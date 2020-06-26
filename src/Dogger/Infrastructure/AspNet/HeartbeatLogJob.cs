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
using Serilog.Events;
using Serilog.Parsing;

namespace Dogger.Infrastructure.AspNet
{
    public class HeartbeatLogJob : TimedHostedService
    {
        public HeartbeatLogJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        protected override TimeSpan Interval => TimeSpan.FromMinutes(3);

        protected override async Task OnTickAsync(
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            var scopeLogger = serviceProvider.GetRequiredService<ILogger>();
            scopeLogger.Verbose("Scoped heartbeat.");

            LoggerFactory.Sink.Emit(new LogEvent(
                DateTimeOffset.Now, 
                LogEventLevel.Verbose,
                null,
                new MessageTemplate("Sink heartbeat.", Array.Empty<MessageTemplateToken>()),
                Array.Empty<LogEventProperty>()));

            Log.Verbose("Static log heartbeat.");

            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var newLogger = LoggerFactory.BuildWebApplicationLogger(configuration);
            newLogger.Verbose("New logger heartbeat.");
        }
    }
}
