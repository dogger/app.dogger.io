using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog.Events;

namespace Dogger.Infrastructure.Logging
{
    [Obsolete("Only used temporarily for debugging. Should be removed when this issue is solved: https://github.com/dogger/dogger.io/issues/372")]
    public class DisposableBugLoggerProxy : ILogEventSink, IDisposable
    {
        private readonly ILogEventSink inner;

        public bool IsDisposed { get; set; }

        public DisposableBugLoggerProxy(
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
            IsDisposed = true;
        }
    }
}
