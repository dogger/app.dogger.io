using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Time;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dogger.Infrastructure.AspNet
{
    public abstract class TimedHostedService : IHostedService
    {
        private Timer? timer;
        private readonly IServiceProvider serviceProvider;

        [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "This field is disposed at the Dispose method.")]
        private IServiceScope? scope;

        protected abstract TimeSpan Interval { get; }

        protected TimedHostedService(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            scope = this.serviceProvider.CreateScope();

            var time = this.scope.ServiceProvider.GetRequiredService<ITime>();
            timer = await time.CreateTimerAsync(
                Interval,
                async () => await TickAsync(stoppingToken));
        }

        private async Task TickAsync(CancellationToken cancellationToken)
        {
            if (this.scope == null)
                throw new InvalidOperationException("Scope was not assigned.");

            await OnTickAsync(scope.ServiceProvider, cancellationToken);
        }

        protected abstract Task OnTickAsync(
            IServiceProvider provider, 
            CancellationToken cancellationToken);

        public async Task StopAsync(CancellationToken stoppingToken)
        {
            this.timer?.Change(Timeout.Infinite, 0);
            this.timer?.Dispose();

            this.scope?.Dispose();
        }
    }
}
