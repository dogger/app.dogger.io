using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure.Time
{
    [ExcludeFromCodeCoverage]
    class Stopwatch : IStopwatch
    {
        private readonly System.Diagnostics.Stopwatch inner;

        public TimeSpan Elapsed => this.inner.Elapsed;

        public Stopwatch(System.Diagnostics.Stopwatch inner)
        {
            this.inner = inner;
        }
    }
}
