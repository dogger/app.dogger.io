using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.Configuration;
using Dogger.Infrastructure.Database;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.Ioc;
using Dogger.Infrastructure.Logging;
using FluffySpoon.AspNet.NGrok;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Octokit;
using Serilog;

namespace Dogger
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "This is the main method.")]
        static async Task<int> Main(string[] args)
        {
            var configuration = ConfigurationFactory.BuildConfiguration("be404feb-b81c-425a-b355-029dbd854c3d", args);
            Log.Logger = LoggerFactory.BuildWebApplicationLogger(configuration);

            try
            {
                var host = CreateDoggerHostBuilder(configuration, args).Build();
                await DatabaseMigrator.MigrateDatabaseForHostAsync(host);

                var dataContext = host.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
                var repositories = await dataContext
                    .PullDogSettings
                    .Include(x => x.User)
                    .ThenInclude(x => x.Identities)
                    .Include(x => x.Repositories)
                    .ToListAsync();

                var gitHubClient = host.Services.GetRequiredService<IGitHubClient>();
                var list = await gitHubClient.GitHubApps.GetAllInstallationsForCurrent();

                throw new InvalidOperationException("Remember to clear secrets");

                //await host.RunAsync();

                //return 0;
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
                    var registry = new IocRegistry(
                        services,
                        context.Configuration);
                    registry.RegisterDelayedHostedServices();
                });

        [SuppressMessage(
            "CodeQuality", 
            "IDE0051:Remove unused private members", 
            Justification = "This is used for Entity Framework when running console commands for migrations etc. It must have this signature.")]
        private static IHostBuilder CreateHostBuilder(string[] args) => CreateDoggerHostBuilder(null, args);
    }
}
