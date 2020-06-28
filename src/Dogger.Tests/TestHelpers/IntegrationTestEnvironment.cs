using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Dogger.Infrastructure;
using FluffySpoon.AspNet.NGrok;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Dogger.Tests.TestHelpers
{
    class EnvironmentSetupOptions
    {
        public string EnvironmentName { get; set; }
        public Action<IServiceCollection> IocConfiguration { get; set; }
        public bool SkipWebServer { get; set; }
    }

    [ExcludeFromCodeCoverage]
    class IntegrationTestEnvironment : IAsyncDisposable
    {
        private readonly IHost host;
        private readonly IServiceScope serviceScope;

        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly EnvironmentSetupOptions options;

        public IServiceProvider ServiceProvider { get; }

        public IMediator Mediator => ServiceProvider.GetRequiredService<Mediator>();
        public DataContext DataContext => ServiceProvider.GetRequiredService<DataContext>();
        public IConfiguration Configuration => ServiceProvider.GetRequiredService<IConfiguration>();

        private IntegrationTestEnvironment(EnvironmentSetupOptions options = null)
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            options ??= new EnvironmentSetupOptions();

            EnvironmentHelper.SetRunningInTestFlag();

            this.host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(ConfigureConfigurationBuilder)
                .UseEnvironment(options.EnvironmentName ?? Environments.Development)
                .ConfigureWebHostDefaults(webBuilder => webBuilder
                    .UseStartup<Startup>()
                    .UseKestrel()
                    .UseUrls("https://*:14569;http://*:14568")
                    .UseNGrok(new NgrokOptions()
                    {
                        ShowNGrokWindow = false,
                        Disable = false,
                        ApplicationHttpUrl = "http://localhost:14568"
                    }))
                .ConfigureServices(services =>
                {
                    TestServiceProviderFactory.ConfigureServicesForTesting(services);
                    options.IocConfiguration?.Invoke(services);
                })
                .Build();

            serviceScope = this.host.Services.CreateScope();
            ServiceProvider = serviceScope.ServiceProvider;

            this.options = options;
        }

        private static void ConfigureConfigurationBuilder(IConfigurationBuilder builder)
        {
            foreach (var source in builder.Sources.ToArray())
            {
                if (source is ChainedConfigurationSource)
                    continue;

                builder.Sources.Remove(source);
            }

            builder
                .AddJsonFile("appsettings.json", false)
                .AddJsonFile($"appsettings.{Environments.Development}.json", false);
        }

        public async Task WithFreshDataContext(Func<DataContext, Task> action)
        {
            await WithFreshDataContext<object>(async (dataContext) =>
            {
                await action(dataContext);
                return null;
            });
        }

        public async Task<T> WithFreshDataContext<T>(Func<DataContext, Task<T>> action)
        {
            using var freshScope = this.host.Services.CreateScope();
            await using var dataContext = freshScope.ServiceProvider.GetRequiredService<DataContext>();

            var result = await action(dataContext);
            await dataContext.SaveChangesAsync();
            return result;
        }

        public static async Task<IntegrationTestEnvironment> CreateAsync(EnvironmentSetupOptions options = null)
        {
            var environment = new IntegrationTestEnvironment(options);
            await environment.InitializeWebServer();

            return environment;
        }

        private async Task InitializeWebServer()
        {
            if (this.options.SkipWebServer)
                return;

            Console.WriteLine("Initializing integration test environment.");

            var hostStartTask = this.host.StartAsync(cancellationTokenSource.Token);

            await WaitForUrlToBeAvailable("http://localhost:14568/health");

            var ngrokService = ServiceProvider.GetService<INGrokHostedService>();
            if (ngrokService != null)
                await WaitForTunnelsToOpenAsync(ngrokService);

            await hostStartTask;
        }

        private static async Task WaitForTunnelsToOpenAsync(INGrokHostedService ngrokService)
        {
            var tunnels = await ngrokService.GetTunnelsAsync();
            Console.WriteLine("Tunnels {0} are now open.", tunnels.Select(x => x.PublicUrl));
        }

        private static async Task WaitForUrlToBeAvailable(string url)
        {
            using var client = new HttpClient();

            var isAvailable = false;
            var stopwatch = Stopwatch.StartNew();
            while (!isAvailable && stopwatch.Elapsed < TimeSpan.FromSeconds(60))
            {
                isAvailable = true;

                try
                {
                    var response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                }
                catch (HttpRequestException)
                {
                    isAvailable = false;
                    await Task.Delay(1000);
                }
            }

            if (!isAvailable)
                throw new InvalidOperationException("The web server didn't start within enough time.");
        }

        public async ValueTask DisposeAsync()
        {
            this.cancellationTokenSource.Cancel();

            await DowngradeDatabaseAsync();

            this.serviceScope.Dispose();
            this.host.Dispose();
        }

        private async Task DowngradeDatabaseAsync()
        {
            try
            {
                await WithFreshDataContext(async dataContext => await dataContext
                    .GetService<IMigrator>()
                    .MigrateAsync(Migration.InitialDatabase));
            }
            catch (SqlException)
            {
                //ignored.
            }
        }
    }
}
