using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluffySpoon.AspNet.NGrok;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dogger.Tests.TestHelpers.Environments.Dogger
{

    public class DoggerStartupEntrypoint : IIntegrationTestEntrypoint
    {
        private readonly IHost host;

        private readonly IServiceScope scope;

        public IServiceProvider RootProvider { get; }
        public IServiceProvider ScopeProvider => this.scope.ServiceProvider;

        private readonly CancellationTokenSource cancellationTokenSource;

        public DoggerStartupEntrypoint(DoggerEnvironmentSetupOptions options)
        {
            this.cancellationTokenSource = new CancellationTokenSource();

            this.host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration(ConfigureConfigurationBuilder)
                .UseEnvironment(options.EnvironmentName ?? Microsoft.Extensions.Hosting.Environments.Development)
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

            this.RootProvider = this.host.Services;

            var serviceScope = this.RootProvider.CreateScope();
            this.scope = serviceScope;
        }

        public async Task WaitUntilReadyAsync()
        {
            Console.WriteLine("Initializing integration test environment.");

            var hostStartTask = this.host.StartAsync(this.cancellationTokenSource.Token);
            await WaitForUrlToBeAvailable("http://localhost:14568/health");

            var ngrokService = this.RootProvider.GetService<INGrokHostedService>();
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
                .AddJsonFile($"appsettings.{Microsoft.Extensions.Hosting.Environments.Development}.json", false);
        }

        public async ValueTask DisposeAsync()
        {
            this.cancellationTokenSource.Cancel();

            this.scope.Dispose();
            this.host.Dispose();
        }
    }

}
