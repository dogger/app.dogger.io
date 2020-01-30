using System;

namespace Dogger.Infrastructure.Time
{
    public interface IStopwatch
    {
        TimeSpan Elapsed { get; }
    }

}
