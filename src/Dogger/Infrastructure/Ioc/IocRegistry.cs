using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Amazon;
using Amazon.ECR;
using Amazon.Extensions.NETCore.Setup;
using Amazon.IdentityManagement;
using Amazon.Lightsail;
using Amazon.Runtime;
using AutoMapper;
using Dogger.Controllers.PullDog.Webhooks.Handlers;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Amazon.Identity;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.States.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance;
using Dogger.Domain.Services.Provisioning.States.RunDockerComposeOnInstance;
using Dogger.Domain.Services.PullDog;
using Dogger.Domain.Services.PullDog.GitHub;
using Dogger.Infrastructure.AspNet;
using Dogger.Infrastructure.AspNet.Options;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Configuration;
using Dogger.Infrastructure.Database;
using Dogger.Infrastructure.Docker.Engine;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Infrastructure.Encryption;
using Dogger.Infrastructure.GitHub;
using Dogger.Infrastructure.IO;
using Dogger.Infrastructure.Mediatr;
using Dogger.Infrastructure.Mediatr.Database;
using Dogger.Infrastructure.Mediatr.Tracing;
using Dogger.Infrastructure.Secrets;
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
using Octokit;
using Serilog;
using Slack.Webhooks;
using Stripe;
using File = Dogger.Infrastructure.IO.File;

namespace Dogger.Infrastructure.Ioc
{
    public class IocRegistry
    {
        protected IServiceCollection Services { get; }
        protected IConfiguration Configuration { get; }

        protected OnPremisesManifest OnPremisesManifest { get; }

        public IocRegistry(
            IServiceCollection services,
            IConfiguration configuration)
        {
            this.Services = services;
            this.Configuration = configuration;

            this.OnPremisesManifest = new OnPremisesManifest(configuration);
        }

        public virtual void Register()
        {
            ConfigureOptions();

            ConfigureDebugHelpers();

            ConfigureInfrastructure();

            ConfigureProvisioning();
            ConfigureAutoMapper();
            ConfigureMediatr(typeof(IocRegistry).Assembly);
            ConfigureDocker();

            ConfigureEntityFramework();

            ConfigureStripe();

            ConfigureAws(); 
            
            ConfigureHealthChecks();

            ConfigureFlurl();

            ConfigureSlack();

            ConfigureGitHub();

            ConfigureAuth0();

            ConfigureLogging();

            ConfigureWebhooks();
        }

        private void ConfigureWebhooks()
        {
            this.Services.AddTransient<IConfigurationPayloadHandler, InstallationConfigurationPayloadHandler>();
            this.Services.AddTransient<IConfigurationPayloadHandler, UninstallationConfigurationPayloadHandler>();

            this.Services.AddTransient<IWebhookPayloadHandler, BotCommandPayloadHandler>();
            this.Services.AddTransient<IWebhookPayloadHandler, PullRequestClosedPayloadHandler>();
            this.Services.AddTransient<IWebhookPayloadHandler, PullRequestReadyPayloadHandler>();
        }

        private void ConfigureLogging()
        {
            this.Services.AddTransient(p => Log.Logger);
        }

        private void ConfigureAuth0()
        {
            this.Services.AddOptionalSingleton<IManagementApiClientFactory, ManagementApiClientFactory>(
                this.OnPremisesManifest.HasStripe);
        }

        private void ConfigureGitHub()
        {
            this.Services.AddTransient(p =>
            {
                var pullDogOptions = GetPullDogOptions();

                var privateKey = 
                    pullDogOptions?.PrivateKey?.Replace("\\n", "\n", StringComparison.InvariantCulture) ??
                    throw new InvalidOperationException("Could not find private key");

                if (string.IsNullOrWhiteSpace(privateKey))
                {
                    return new GitHubClient(new ProductHeaderValue("pull-dog"));
                }

                return ConstructGitHubClientWithPrivateKey(privateKey);
            });

            this.Services.AddTransient<IGitHubClientFactory, GitHubClientFactory>();
            this.Services.AddTransient<IPullDogRepositoryClientFactory, GitHubPullDogRepositoryClientFactory>();
            this.Services.AddTransient<IPullDogFileCollectorFactory, PullDogFileCollectorFactory>();
        }

        private GitHubPullDogOptions GetPullDogOptions()
        {
            var options = this.Configuration.GetSection<GitHubOptions>();
            if (options.PullDog == null)
            {
                throw new InvalidOperationException("Could not find GitHub Pull Dog options.");
            }

            return options.PullDog;
        }

        [ExcludeFromCodeCoverage]
        private IGitHubClient ConstructGitHubClientWithPrivateKey(string privateKey)
        {
            var pullDogOptions = GetPullDogOptions();

            var appIdentifier = 
                pullDogOptions.AppIdentifier ??
                throw new InvalidOperationException("Could not find app identifier.");

            var privateKeySource = new StringPrivateKeySource(privateKey);

            var tokenFactory = new GitHubJwtFactory(
                privateKeySource,
                new GitHubJwtFactoryOptions()
                {
                    AppIntegrationId = appIdentifier,
                    ExpirationSeconds = 60 * 5
                });

            var token = tokenFactory.CreateEncodedJwtToken();
            return new GitHubClient(new ProductHeaderValue("pull-dog"))
            {
                Credentials = new Credentials(
                    token,
                    AuthenticationType.Bearer)
            };
        }

        private void ConfigureSlack()
        {
            var slackSettings = this.Configuration.GetSection<SlackOptions>();
            var incomingUrl = slackSettings?.IncomingUrl;

            this.Services.AddOptionalSingleton<ISlackClient, SlackClient>(
                _ => new SlackClient(incomingUrl),
                incomingUrl != null);
        }

        private void ConfigureFlurl()
        {
            this.Services.AddSingleton<IFlurlClientFactory, PerBaseUrlFlurlClientFactory>();
        }

        private void ConfigureHealthChecks()
        {
            this.Services.AddHealthChecks()
                .AddDbContextCheck<DataContext>();
        }

        private void ConfigureOptions()
        {
            void Configure<TOptions>() where TOptions : class
            {
                var configurationKey = Configuration.GetSectionNameFor<TOptions>();
                this.Services.Configure<TOptions>(this.Configuration.GetSection(configurationKey));
            }

            this.Services.AddOptions();

            Configure<AwsOptions>();
            Configure<GitHubOptions>();
            Configure<SqlOptions>();
            Configure<StripeOptions>();
            Configure<EncryptionOptions>();
            Configure<Auth0Options>();
        }

        private void ConfigureDebugHelpers()
        {
            var shouldConfigure =
                Debugger.IsAttached ||
                EnvironmentHelper.IsRunningInTest;
            if (!shouldConfigure)
                return;

            DockerDependencyService.InjectInto(
                this.Services,
                this.Configuration);
        }

        private void ConfigureStripe()
        {
            var isStripeConfigured = this.OnPremisesManifest.HasStripe;

            var stripeConfiguration = this.Configuration.GetSection<StripeOptions>();

            var secretKey = stripeConfiguration?.SecretKey;
            var publishableKey = stripeConfiguration?.PublishableKey;

            this.Services.AddOptionalSingleton<CustomerService>(isStripeConfigured);
            this.Services.AddOptionalSingleton<PaymentMethodService>(isStripeConfigured);
            this.Services.AddOptionalSingleton<SubscriptionService>(isStripeConfigured);
            this.Services.AddOptionalSingleton<WebhookEndpointService>(isStripeConfigured);

            this.Services.AddOptionalSingleton<IStripeClient, StripeClient>(
                _ => new StripeClient(
                    apiKey: secretKey,
                    clientId: publishableKey),
                () => isStripeConfigured);
        }

        [ExcludeFromCodeCoverage]
        private void ConfigureEntityFramework()
        {
            this.Services.AddDbContextPool<DataContext>(
                optionsBuilder =>
                {
                    var sqlOptions = this.Configuration.GetSection<SqlOptions>();
                    var connectionString = sqlOptions?.ConnectionString;

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
                1);

            this.Services.AddScoped<IDatabaseMigrator, DatabaseMigrator>();
        }

        private void ConfigureDocker()
        {
            this.Services.AddTransient<IDockerEngineClientFactory, DockerEngineClientFactory>();
        }

        private void ConfigureInfrastructure()
        {
            this.Services.AddSingleton<ISshClientFactory, SshClientFactory>();
            this.Services.AddSingleton<IAesEncryptionHelper, AesEncryptionHelper>();
            this.Services.AddSingleton<ISecretsScanner, SecretsScanner>();

            this.Services.AddSingleton<IFile, File>();

            this.Services.AddSingleton<ITime, Time.Time>();
            this.Services.AddSingleton<ITimeProvider, TimeProvider>();

            this.Services.AddScoped<IDockerComposeParserFactory, DockerComposeParserFactory>();

            this.Services.AddLogging(builder => builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddConsole());

            this.Services.AddSingleton(this.Configuration);
        }

        public void ConfigureMediatr(params Assembly[] assemblies)
        {
            this.Services.AddMediatR(x => x.AsTransient(), assemblies);

            this.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            this.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(DatabaseTransactionBehavior<,>));
            this.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TracingBehavior<,>));
        }

        private void ConfigureAutoMapper()
        {
            this.Services.AddAutoMapper(typeof(IocRegistry).Assembly);
        }

        private void ConfigureAws()
        {
            BasicAWSCredentials CreateAwsCredentials()
            {
                var options = this.Configuration.GetSection<AwsOptions>();

                var accessKey = options.AccessKeyId;
                if (accessKey == null)
                    throw new InvalidOperationException("The AWS access key ID could not be found.");

                var secretKey = options.SecretAccessKey;
                if (secretKey == null)
                    throw new InvalidOperationException("The AWS secret access key could not be found.");

                return new BasicAWSCredentials(
                    accessKey,
                    secretKey);
            }

            AWSOptions CreateAwsOptions(RegionEndpoint region)
            {
                var options = this.Configuration.GetAWSOptions();

                options.Region = region;
                options.Credentials = CreateAwsCredentials();

                return options;
            }

            this.Services.AddDefaultAWSOptions(
                CreateAwsOptions(RegionEndpoint.EUWest1));

            this.Services.AddScoped<IUserAuthenticatedServiceFactory<IAmazonECR>, UserAuthenticatedEcrServiceFactory>();

            this.Services.AddAWSService<IAmazonLightsail>();
            this.Services.AddAWSService<IAmazonECR>();
            this.Services.AddAWSService<IAmazonIdentityManagementService>();

            this.Services.AddSingleton<ILightsailOperationService, LightsailOperationService>();

            //we register an additional Amazon lightsail client just for "us-east-1", because that is the only place where domain related APIs work.
            this.Services.AddSingleton<IAmazonLightsailDomain>(provider => 
                new AmazonLightsailDomainClient(
                    CreateAwsCredentials(),
                    RegionEndpoint.USEast1));
        }

        private void ConfigureProvisioning()
        {
            this.Services.AddSingleton<ProvisioningService>();
            this.Services.AddSingleton<IProvisioningService>(x => x.GetRequiredService<ProvisioningService>());

            this.Services.AddTransient<CreateLightsailInstanceState>();
            this.Services.AddTransient<InstallSoftwareOnInstanceState>();
            this.Services.AddTransient<RunDockerComposeOnInstanceState>();
            this.Services.AddTransient<CompleteInstanceSetupState>();
        }

        [ExcludeFromCodeCoverage]
        public void RegisterDelayedHostedServices()
        {
            this.Services.AddHostedService(p => p.GetRequiredService<ProvisioningService>());
        }
    }
}
