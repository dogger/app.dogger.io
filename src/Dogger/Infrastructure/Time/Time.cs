using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Time
{
    [ExcludeFromCodeCoverage]
    public class Time : ITime
    {
        public async Task<Timer> CreateTimerAsync(
            TimeSpan interval,
            Func<Task> callback)
        {
            return new Timer(
                async _ => await callback(),
                null,
                interval,
                interval);
        }

        public async Task WaitAsync(int milliseconds)
        {
            await Task.Delay(milliseconds);
        }

        public IStopwatch StartStopwatch()
        {
            return new Stopwatch(
                System.Diagnostics.Stopwatch.StartNew());
        }
    }
}
