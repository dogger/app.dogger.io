using System;
using System.Diagnostics;
using Dogger.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dogger.Infrastructure.Database;
using Dogger.Infrastructure.Logging;
using FluffySpoon.AspNet.NGrok;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;

namespace Dogger
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main method.")]
        static async Task<int> Main(string[] args)
        {
            var configuration = ConfigurationFactory.BuildConfiguration(args);
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
    }
}
