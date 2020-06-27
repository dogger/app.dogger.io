using System;
using Serilog.Core;
using Serilog.Events;

namespace Dogger.Infrastructure.Logging
{
    public class NonDisposableSink : ILogEventSink, IDisposable
    {
        private readonly ILogEventSink inner;

        public NonDisposableSink(
            ILogEventSink inner)
        {
            this.inner = inner;
        }

        public void Emit(LogEvent logEvent)
        {
            inner.Emit(logEvent);
        }

        public void Dispose()
        {
        }
    }
}
