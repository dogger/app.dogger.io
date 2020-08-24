using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Time
{
    public interface ITime
    {
        Timer CreateTimer(
            TimeSpan interval,
            Func<Task> callback);

        Task WaitAsync(int milliseconds);
        IStopwatch StartStopwatch();
    }

}
