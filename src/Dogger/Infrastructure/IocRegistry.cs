using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using Amazon;
using Amazon.ECR;
using Amazon.Extensions.NETCore.Setup;
using Amazon.IdentityManagement;
using Amazon.Lightsail;
using Amazon.Runtime;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Amazon.Identity;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.Dogfeeding;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Stages.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance;
using Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance;
using Dogger.Domain.Services.PullDog;
using Dogger.Domain.Services.PullDog.GitHub;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.AspNet.Options;
using Dogger.Infrastructure.AspNet.Options.Dogfeed;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Database;
using Dogger.Infrastructure.Docker.Engine;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Infrastructure.Encryption;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.Mediatr;
using Dogger.Infrastructure.Mediatr.Database;
using Dogger.Infrastructure.Ssh;
using Dogger.Infrastructure.Time;
using Flurl.Http.Configuration;
using GitHubJwt;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Octokit;
using Slack.Webhooks;
using Stripe;

namespace Dogger.Infrastructure
{
    public static class IocRegistry
    {
        public static void Register(
            IServiceCollection services,
            IConfiguration configuration)
        {
            ConfigureDebugHelpers(
                services, 
                configuration);

            ConfigureInfrastructure(
                services, 
                configuration);

            ConfigureOptions(
                services, 
                configuration);

            ConfigureProvisioning(services);
            ConfigureAutoMapper(services);
            ConfigureMediatr(services, typeof(IocRegistry).Assembly);
            ConfigureDocker(services);

            ConfigureEntityFramework(
                services,
                configuration);

            ConfigureStripe(
                services, 
                configuration);

            ConfigureAws(
                services,
                configuration); 
            
            ConfigureHealthChecks(services);

            ConfigureFlurl(services);

            ConfigureSlack(
                services,
                configuration);

            ConfigureGitHub(
                services,
                configuration);

            ConfigureAuth0(services);

            ConfigureLogging(services);

            ConfigureWebhooks(services);
        }

        private static void ConfigureWebhooks(IServiceCollection services)
        {
            services.AddTransient<IConfigurationPayloadHandler, InstallationConfigurationPayloadHandler>();
            services.AddTransient<IConfigurationPayloadHandler, UninstallationConfigurationPayloadHandler>();

            services.AddTransient<IWebhookPayloadHandler, BotCommandPayloadHandler>();
            services.AddTransient<IWebhookPayloadHandler, PullRequestClosedPayloadHandler>();
            services.AddTransient<IWebhookPayloadHandler, PullRequestReadyPayloadHandler>();
        }

        private static void ConfigureLogging(IServiceCollection services)
        {
            services.AddTransient(p => Log.Logger);
        }

        private static void ConfigureAuth0(IServiceCollection services)
        {
            services.AddSingleton<IManagementApiClientFactory, ManagementApiClientFactory>();
        }

        private static void ConfigureGitHub(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddTransient<IGitHubClient>(p =>
            {
                var privateKey = configuration["GitHub:PullDog:PrivateKey"]
                    .Replace("\\n", "\n", StringComparison.InvariantCulture);
                if (string.IsNullOrWhiteSpace(privateKey))
                {
                    return new GitHubClient(new ProductHeaderValue("pull-dog"));
                }

                var privateKeySource = new StringPrivateKeySource(
                    privateKey);

                var tokenFactory = new GitHubJwtFactory(
                    privateKeySource,
                    new GitHubJwtFactoryOptions()
                    {
                        AppIntegrationId = int.Parse(
                            configuration["GitHub:PullDog:AppIdentifier"],
                            CultureInfo.InvariantCulture),
                        ExpirationSeconds = 60 * 5
                    });

                var token = tokenFactory.CreateEncodedJwtToken();
                return new GitHubClient(new ProductHeaderValue("pull-dog"))
                {
                    Credentials = new Credentials(
                        token,
                        AuthenticationType.Bearer)
                };
            });

            services.AddTransient<IGitHubClientFactory, GitHubClientFactory>();
            services.AddTransient<IPullDogRepositoryClientFactory, GitHubPullDogRepositoryClientFactory>();
            services.AddTransient<IPullDogFileCollectorFactory, PullDogFileCollectorFactory>();
        }

        private static void ConfigureSlack(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddSingleton<ISlackClient>(_ => new SlackClient(configuration["Slack:IncomingUrl"]));
        }

        private static void ConfigureFlurl(IServiceCollection services)
        {
            services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
        }

        private static void ConfigureHealthChecks(IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddDbContextCheck<DataContext>();
        }

        private static void ConfigureOptions(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            void Configure<TOptions>() where TOptions : class
            {
                const string optionsSuffix = "Options";

                var configurationKey = typeof(TOptions).Name;
                if (configurationKey.EndsWith(optionsSuffix, StringComparison.InvariantCulture))
                {
                    configurationKey = configurationKey.Replace(
                        optionsSuffix, 
                        string.Empty, 
                        StringComparison.InvariantCulture);
                }

                services.Configure<TOptions>(configuration.GetSection(configurationKey));
            }

            services.AddOptions();

            Configure<AwsOptions>();
            Configure<CloudflareOptions>();
            Configure<GitHubOptions>();
            Configure<DogfeedOptions>();
            Configure<SqlOptions>();
            Configure<StripeOptions>();
            Configure<EncryptionOptions>();
            Configure<Auth0Options>();
        }

        private static void ConfigureDebugHelpers(IServiceCollection services, IConfiguration configuration)
        {
            var shouldConfigure =
                Debugger.IsAttached ||
                EnvironmentHelper.IsRunningInTest;
            if (!shouldConfigure)
                return;

            DockerDebugEnvironmentService.InjectInto(
                services,
                configuration);
        }

        private static void ConfigureStripe(
            IServiceCollection services, 
            IConfiguration configuration)
        {
            services.AddSingleton<IStripeClient>(p =>
                new StripeClient(
                    apiKey: configuration["Stripe:SecretKey"],
                    clientId: configuration["Stripe:PublishableKey"]));

            services.AddSingleton<CustomerService>();
            services.AddSingleton<PaymentMethodService>();
            services.AddSingleton<SubscriptionService>();
            services.AddSingleton<CardService>();
            services.AddSingleton<WebhookEndpointService>();
        }

        [ExcludeFromCodeCoverage]
        private static void ConfigureEntityFramework(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<DataContext>(
                optionsBuilder =>
                {
                    var connectionString = configuration["Sql:ConnectionString"];

                    var hasConnectionString = !string.IsNullOrEmpty(connectionString);
                    if (hasConnectionString)
                    {
                        optionsBuilder.UseSqlServer(
                            connectionString, 
                            options => options
                                .EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), Array.Empty<int>()));
                    }
                    else
                    {
                        optionsBuilder
                            .UseInMemoryDatabase("dogger")
                            .ConfigureWarnings(w => w
                                .Ignore(InMemoryEventId.TransactionIgnoredWarning));
                    }
                },
                ServiceLifetime.Scoped,
                ServiceLifetime.Scoped);

            services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        }

        private static void ConfigureDocker(IServiceCollection services)
        {
            services.AddTransient<IDockerEngineClientFactory, DockerEngineClientFactory>();
        }

        public static void ConfigureDogfeeding(
            IServiceCollection services)
        {
            services.AddTransient<IDogfeedService, DogfeedService>();
        }

        private static void ConfigureInfrastructure(IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ISshClientFactory, SshClientFactory>();
            services.AddSingleton<IAesEncryptionHelper, AesEncryptionHelper>();

            services.AddSingleton<ITime, Time.Time>();
            services.AddSingleton<ITimeProvider, TimeProvider>();

            services.AddScoped<IDockerComposeParserFactory, DockerComposeParserFactory>();

            services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole());

            services.AddSingleton(configuration);
        }

        public static void ConfigureMediatr(
            IServiceCollection services, 
            params Assembly[] assemblies)
        {
            services.AddMediatR(x => x.AsTransient(), assemblies);

            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DatabaseTransactionBehavior<,>));
        }

        private static void ConfigureAutoMapper(IServiceCollection services)
        {
            services.AddSingleton(x => AutoMapperFactory.CreateValidMapper());
        }

        private static void ConfigureAws(
            IServiceCollection services,
            IConfiguration configuration)
        {
            BasicAWSCredentials CreateAwsCredentials()
            {
                var accessKey = configuration["Aws:AccessKeyId"];
                if (accessKey == null)
                    throw new InvalidOperationException("The AWS access key ID could not be found.");

                var secretKey = configuration["Aws:SecretAccessKey"];
                if (secretKey == null)
                    throw new InvalidOperationException("The AWS secret access key could not be found.");

                return new BasicAWSCredentials(
                    accessKey,
                    secretKey);
            }

            AWSOptions CreateAwsOptions(RegionEndpoint region)
            {
                var options = configuration.GetAWSOptions();

                options.Region = region;
                options.Credentials = CreateAwsCredentials();

                return options;
            }

            services.AddDefaultAWSOptions(
                CreateAwsOptions(RegionEndpoint.EUWest1));

            services.AddScoped<IUserAuthenticatedServiceFactory<IAmazonECR>, UserAuthenticatedEcrServiceFactory>();

            services.AddAWSService<IAmazonLightsail>();
            services.AddAWSService<IAmazonECR>();
            services.AddAWSService<IAmazonIdentityManagementService>();

            services.AddSingleton<ILightsailOperationService, LightsailOperationService>();

            //we register an additional Amazon lightsail client just for "us-east-1", because that is the only place where domain related APIs work.
            services.AddSingleton<IAmazonLightsailDomain>(provider => 
                new AmazonLightsailDomainClient(
                    CreateAwsCredentials(),
                    RegionEndpoint.USEast1));
        }

        private static void ConfigureProvisioning(IServiceCollection services)
        {
            services.AddSingleton<ProvisioningService>();
            services.AddSingleton<IProvisioningService>(x => x.GetRequiredService<ProvisioningService>());

            services.AddTransient<CreateLightsailInstanceStage>();
            services.AddTransient<InstallSoftwareOnInstanceStage>();
            services.AddTransient<RunDockerComposeOnInstanceStage>();
        }

        [ExcludeFromCodeCoverage]
        public static void RegisterDelayedHostedServices(IServiceCollection services)
        {
            services.AddHostedService(p => p.GetRequiredService<ProvisioningService>());
        }
    }
}
