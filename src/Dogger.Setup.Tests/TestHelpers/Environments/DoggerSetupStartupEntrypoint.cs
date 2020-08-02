using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Dogger.Infrastructure.AspNet;
using Dogger.Setup.Infrastructure;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Dogger.Setup.Tests.TestHelpers.Environments
{
    class DoggerSetupStartupEntrypoint : IIntegrationTestEntrypoint
    {
        private DockerDependencyService dockerDependencyService;

        public DoggerSetupStartupEntrypoint(DoggerSetupEnvironmentSetupOptions options)
        {
            var configurationBuilder = new ConfigurationBuilder();
            TestConfigurationFactory.ConfigureConfigurationBuilder(configurationBuilder);

            var configuration = configurationBuilder.Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(Substitute.For<IHttpContextAccessor>());

            IocRegistry.Register(serviceCollection, configuration);
            DockerDependencyService.InjectInto(serviceCollection, configuration);
            TestServiceProviderFactory.ConfigureServicesForTesting(serviceCollection);

            options.IocConfiguration?.Invoke(serviceCollection);

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
