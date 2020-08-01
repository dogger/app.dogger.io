using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dogger.Infrastructure.AspNet;
using Dogger.Tests.TestHelpers.Environments;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Setup.Tests.TestHelpers.Environments
{
    class DoggerSetupStartupEntrypoint : IIntegrationTestEntrypoint
    {
        private DockerDependencyService dockerDependencyService;

        public DoggerSetupStartupEntrypoint()
        {
            var serviceCollection = new ServiceCollection();
            RootProvider = serviceCollection.BuildServiceProvider();
            ScopeProvider = RootProvider.CreateScope().ServiceProvider;
        }

        public async ValueTask DisposeAsync()
        {
            await this.dockerDependencyService.StopAsync(default);
        }

        public IServiceProvider RootProvider { get; }
        public IServiceProvider ScopeProvider { get; }

        public async Task WaitUntilReadyAsync()
        {
            this.dockerDependencyService = RootProvider.GetRequiredService<DockerDependencyService>();
            await this.dockerDependencyService.StartAsync(default);
        }
    }
}
