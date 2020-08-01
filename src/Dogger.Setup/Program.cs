using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Logging;
using Dogger.Setup.Domain.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Serilog;
using IocRegistry = Dogger.Setup.Infrastructure.IocRegistry;

namespace Dogger.Setup
{
    class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main method.")]
        static async Task<int> Main(string[] args)
        {
            var configuration = ConfigurationFactory.BuildConfiguration(args);
            Log.Logger = LoggerFactory.BuildDogfeedLogger();

            try
            {
                await DogfeedAsync(configuration);

                return 0;
            }
            catch (Exception ex) when (!Debugger.IsAttached)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task DogfeedAsync(IConfiguration configuration)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(configuration);
            serviceCollection.AddSingleton(Substitute.For<IHttpContextAccessor>());

            IocRegistry.Register(
                serviceCollection,
                configuration);

            await using var serviceProvider = serviceCollection.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var dogfeedService = scope
                .ServiceProvider
                .GetRequiredService<IDogfeedService>();

            await dogfeedService.DogfeedAsync();
        }
    }
}
