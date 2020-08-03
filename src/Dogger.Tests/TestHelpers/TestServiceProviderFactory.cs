using System;
using System.Linq;
using Amazon.IdentityManagement;
using Amazon.Lightsail;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Infrastructure;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.Ioc;
using Dogger.Infrastructure.Time;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace Dogger.Tests.TestHelpers
{
    public class TestServiceProviderFactory
    {
        public static IServiceProvider CreateUsingStartup(Action<IServiceCollection> configure = null)
        {
            var services = new ServiceCollection();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.Development.json")
                .AddEnvironmentVariables()
                .Build();

            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns("Development");
            services.AddSingleton(environment);

            var startup = new Startup(
                configuration,
                environment);
            startup.ConfigureServices(services);

            ConfigureServicesForTesting(services);

            configure?.Invoke(services);

            return services.BuildServiceProvider();
        }

        public static void ConfigureServicesForTesting(
            IServiceCollection services)
        {
            RemoveTimedHostedServices(services);
            ConfigureAmazonLightsailDefaultFakes(services);
            ConfigureAmazonIdentityDefaultFakes(services);
            ConfigureFakeDelay(services);

            IocRegistry.ConfigureMediatr(services,
                typeof(TestServiceProviderFactory).Assembly);

            services.AddScoped<Mediator>();
        }

        private static void ConfigureAmazonIdentityDefaultFakes(IServiceCollection services)
        {
            services.RemoveAll<IAmazonIdentityManagementService>();
            services.AddSingleton(Substitute.For<IAmazonIdentityManagementService>());
        }

        private static void ConfigureFakeDelay(IServiceCollection services)
        {
            services.AddSingleton(Substitute.For<ITime>());
        }

        private static void RemoveTimedHostedServices(IServiceCollection services)
        {
            var hostedServiceTypes = services.Where(x => x.ServiceType == typeof(IHostedService));
            var servicesToRemove = hostedServiceTypes.Where(x => x.ImplementationType?.BaseType == typeof(TimedHostedService));
            foreach (var service in servicesToRemove.ToArray())
            {
                services.Remove(service);
            }
        }

        private static void ConfigureAmazonLightsailDefaultFakes(IServiceCollection services)
        {
            services.RemoveAll<IAmazonLightsail>();
            services.RemoveAll<IAmazonLightsailDomain>();

            services.AddSingleton(Substitute.For<IAmazonLightsail>());
            services.AddSingleton(Substitute.For<IAmazonLightsailDomain>());
        }
    }
}
