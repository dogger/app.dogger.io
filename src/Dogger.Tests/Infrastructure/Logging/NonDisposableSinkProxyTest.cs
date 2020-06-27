using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dogger.Infrastructure.Logging;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Dogger.Tests.Infrastructure.Logging
{
    [TestClass]
    public class NonDisposableSinkProxyTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Emit_WithGivenLogEvent_ForwardsCallToInnerSink()
        {
            //Arrange
            var fakeInnerSink = Substitute.For<ILogEventSink>();

            var sinkProxy = new NonDisposableSinkProxy(fakeInnerSink);

            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow, 
                LogEventLevel.Fatal,
                null,
                new MessageTemplate(
                    "dummy", 
                    Array.Empty<MessageTemplateToken>()), 
                Array.Empty<LogEventProperty>());

            //Act
            sinkProxy.Emit(logEvent);

            //Assert
            fakeInnerSink
                .Received(1)
                .Emit(logEvent);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Dispose_InnerDisposableProvided_DoesNotCallDisposeOnInnerSink()
        {
            //Arrange
            var fakeInnerSink = Substitute.For<ITestSink>();

            var sinkProxy = new NonDisposableSinkProxy(fakeInnerSink);

            //Act
            sinkProxy.Dispose();

            //Assert
            fakeInnerSink
                .DidNotReceive()
                .Dispose();
        }

        interface ITestSink : IDisposable, ILogEventSink {}
    }
}
