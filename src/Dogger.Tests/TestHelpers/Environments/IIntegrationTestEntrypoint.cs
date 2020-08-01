using System;
using System.Threading.Tasks;

namespace Dogger.Tests.TestHelpers.Environments
{
    public interface IIntegrationTestEntrypoint : IAsyncDisposable
    {
        public IServiceProvider RootProvider { get; }
        public IServiceProvider ScopeProvider { get; }

        Task WaitUntilReadyAsync();
    }
}
