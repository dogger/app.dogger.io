using System;

namespace Dogger.Infrastructure.Time
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
