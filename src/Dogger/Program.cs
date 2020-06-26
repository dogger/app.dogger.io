using System;
using System.Diagnostics;
using Dogger.Domain.Services.Dogfeeding;
using Dogger.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Destructurama;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Database;
using FluffySpoon.AspNet.NGrok;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Slack.Core;

namespace Dogger
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main method.")]
        public static async Task<int> Main(string[] args)
        {
            var configuration = BuildConfiguration(args);

            if (DogfeedService.IsInDogfeedMode)
            {
                Log.Logger = LoggerFactory.BuildDogfeedLogger();

                await DogfeedAsync(configuration);
                return 0;
            }
            else
            {
                Log.Logger = LoggerFactory.BuildWebApplicationLogger(configuration);

                try
                {
                    var host = CreateDoggerHostBuilder(configuration, args).Build();
                    await DatabaseMigrator.MigrateDatabaseForHostAsync(host);

                    await host.RunAsync();

                    return 0;
                }
                catch (Exception ex) when(!Debugger.IsAttached)
                {
                    Log.Fatal(ex, "Host terminated unexpectedly");
                    return 1;
                }
                finally
                {
                    Log.CloseAndFlush();
                }
            }
        }

        private static IConfigurationRoot BuildConfiguration(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder();

            if(DogfeedService.IsInDogfeedMode)
                DogfeedService.MoveDogfeedPrefixedEnvironmentVariableIntoConfiguration(configurationBuilder);

            configurationBuilder.AddJsonFile("appsettings.json");
            configurationBuilder.AddEnvironmentVariables();
            configurationBuilder.AddCommandLine(args);

            if (Debugger.IsAttached)
            {
                configurationBuilder.AddJsonFile("appsettings.Development.json");
                configurationBuilder.AddUserSecrets("be404feb-b81c-425a-b355-029dbd854c3d");
            }

            var configuration = configurationBuilder.Build();
            return configuration;
        }

        public static IHostBuilder CreateDoggerHostBuilder(IConfiguration? configuration, string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    if (configuration != null)
                        webBuilder.UseConfiguration(configuration);

                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseNGrok(new NgrokOptions()
                    {
                        Disable = !Debugger.IsAttached,
                        ShowNGrokWindow = false
                    });
                })
                .ConfigureServices((context, services) =>
                {
                    IocRegistry.RegisterDelayedHostedServices(services);
                });

        /// <summary>
        /// Used by Entity Framework when running console commands for migrations etc. It must have this signature.
        /// </summary>
        private static IHostBuilder CreateHostBuilder(string[] args) => CreateDoggerHostBuilder(null, args);

        private static async Task DogfeedAsync(IConfiguration configuration)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(configuration);

            IocRegistry.Register(
                serviceCollection,
                configuration);

            IocRegistry.ConfigureDogfeeding(
                serviceCollection);

            await using var serviceProvider = serviceCollection.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var dogfeedService = scope
                .ServiceProvider
                .GetRequiredService<IDogfeedService>();

            await dogfeedService.DogfeedAsync();
        }
    }
}
