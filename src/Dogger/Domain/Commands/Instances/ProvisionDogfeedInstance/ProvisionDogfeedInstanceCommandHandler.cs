using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.AspNet.Options.Dogfeed;
using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.Options;
using Instance = Dogger.Domain.Models.Instance;

namespace Dogger.Domain.Commands.Instances.ProvisionDogfeedInstance
{
    public class ProvisionDogfeedInstanceCommandHandler : IRequestHandler<ProvisionDogfeedInstanceCommand, IProvisioningJob>
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;

        private readonly IOptionsMonitor<DogfeedOptions> dogfeedOptionsMonitor;

        private readonly ILogger logger;

        private readonly DataContext dataContext;

        public ProvisionDogfeedInstanceCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            IConfiguration configuration,
            IOptionsMonitor<DogfeedOptions> dogfeedOptionsMonitor,
            ILogger logger,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.logger = logger;
            this.dataContext = dataContext;
            this.configuration = configuration;
            this.dogfeedOptionsMonitor = dogfeedOptionsMonitor;
        }

        public async Task<IProvisioningJob> Handle(ProvisionDogfeedInstanceCommand request, CancellationToken cancellationToken)
        {
            var dogfeedOptions = this.dogfeedOptionsMonitor.CurrentValue;
            if (dogfeedOptions.DockerComposeYmlContents == null)
                throw new InvalidOperationException("Could not find Docker Compose YML contents.");

            var dockerHubOptions = dogfeedOptions.DockerHub;
            if (dockerHubOptions?.Username == null)
                throw new InvalidOperationException("Could not find Docker Hub username.");

            if (dockerHubOptions.Password == null)
                throw new InvalidOperationException("Could not find Docker Hub password.");

            var cluster = await mediator.Send(new EnsureClusterWithIdCommand(DataContext.DoggerClusterId), cancellationToken);

            var firstCapablePlan = await GetDogfeedingPlanAsync();
            var instance = new Instance()
            {
                Name = request.InstanceName,
                Cluster = cluster,
                IsProvisioned = false,
                PlanId = firstCapablePlan.Id,
                Type = InstanceType.DockerCompose
            };

            cluster.Instances.Add(instance);
            await this.dataContext.Instances.AddAsync(instance, cancellationToken);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            var dockerComposeYmlContents = SanitizeFileContentsFromConfiguration(dogfeedOptions.DockerComposeYmlContents);
            logger.Debug("Docker Compose YML file contents are {DockerComposeYmlFileContents}.", dockerComposeYmlContents);

            var dockerFiles = GetDockerFiles(this.configuration, dogfeedOptions);

            return await this.provisioningService.ScheduleJobAsync(
                new AggregateProvisioningStageFlow(
                    new ProvisionInstanceStageFlow(
                        firstCapablePlan.Id,
                        instance),
                    new DeployToClusterStageFlow(
                        request.InstanceName,
                        new[] { dockerComposeYmlContents })
                    {
                        Files = dockerFiles,
                        Authentication = new[] {
                            new DockerAuthenticationArguments(
                                username: dockerHubOptions.Username,
                                password: dockerHubOptions.Password)
                        }
                    }));
        }

        private async Task<Plan> GetDogfeedingPlanAsync()
        {
            var allPlans = await mediator.Send(new GetSupportedPlansQuery());

            var firstCapablePlan = allPlans
                .OrderBy(x => x.PriceInHundreds)
                .First(x => x.Bundle.RamSizeInGb >= 4);
            return firstCapablePlan;
        }

        /// <summary>
        /// Makes the job create a file on the disk called environment-variables.env, which is then referenced by docker-compose.deploy.yml.
        /// </summary>
        private static InstanceDockerFile[] GetDockerFiles(
            IConfiguration configuration,
            DogfeedOptions options)
        {
            var elasticsearchOptions = options.Elasticsearch;
            if (elasticsearchOptions == null)
                throw new InvalidOperationException("Could not find Elasticsearch options.");

            var instanceEnvironmentVariableFile = GetInstanceEnvironmentVariableFile(configuration);

            var elasticsearchInstancePassword =
                elasticsearchOptions.InstancePassword ??
                throw new InvalidOperationException("No Elasticsearch instance password was specified.");

            return new[]
            {
                instanceEnvironmentVariableFile,
                new InstanceDockerFile(
                    "env/elasticsearch.env",
                    FormatEnvironmentVariableFileContentsFromValues(new Dictionary<string, string>()
                    {
                        { "node.name", "elasticsearch" },
                        { "discovery.type", "single-node" },
                        { "ES_JAVA_OPTS", "-Xms256m -Xmx256m" },
                        { "ROOT_CA", "root-ca.pem" },
                        { "ADMIN_PEM", "admin.pem" },
                        { "ADMIN_KEY", "admin.key" },
                        {
                            "ADMIN_KEY_PASS",
                            elasticsearchOptions.AdminKeyPassword ??
                                throw new InvalidOperationException("No admin key password was specified.")
                        },
                        { "ELASTIC_PWD", "elastic" },
                        { "KIBANA_PWD", elasticsearchInstancePassword }
                    })),
                new InstanceDockerFile(
                    "env/kibana.env",
                    FormatEnvironmentVariableFileContentsFromValues(new Dictionary<string, string>()
                    {
                        { "ELASTICSEARCH_HOSTS", "https://elasticsearch:9200" },
                        { "ELASTICSEARCH_SSL_VERIFICATIONMODE", "none" },
                        { "ELASTICSEARCH_USERNAME", "kibana" },
                        { "ELASTICSEARCH_PASSWORD", elasticsearchInstancePassword }
                    })),
                new InstanceDockerFile(
                    "certs/admin.key",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.AdminKeyContents)),
                new InstanceDockerFile(
                    "certs/admin.pem",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.AdminPemContents)),
                new InstanceDockerFile(
                    "certs/node.key",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.NodeKeyContents)),
                new InstanceDockerFile(
                    "certs/node.pem",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.NodePemContents)),
                new InstanceDockerFile(
                    "certs/root-ca.key",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.RootCaKeyContents)),
                new InstanceDockerFile(
                    "certs/root-ca.pem",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.RootCaPemContents)),
                new InstanceDockerFile(
                    "config/elasticsearch.yml",
                    SanitizeFileContentsFromConfiguration(elasticsearchOptions.ConfigurationYmlContents))
            };
        }

        private static InstanceDockerFile GetInstanceEnvironmentVariableFile(IConfiguration configuration)
        {
            const string instanceEnvironmentVariableKeyPrefix = "INSTANCE_";

            var configurationAsKeyValuePairs = configuration
                .AsEnumerable()
                .ToArray();

            var configurationToTransferToInstance = configurationAsKeyValuePairs
                .Where(x => x.Key.StartsWith(
                    instanceEnvironmentVariableKeyPrefix,
                    StringComparison.InvariantCulture))
                .ToDictionary(
                    x => x.Key.Substring(
                        instanceEnvironmentVariableKeyPrefix.Length),
                    x => x.Value);

            var instanceEnvironmentVariableFile = new InstanceDockerFile(
                "env/dogger.env",
                FormatEnvironmentVariableFileContentsFromValues(configurationToTransferToInstance));
            return instanceEnvironmentVariableFile;
        }

        static string FormatEnvironmentVariableFileContentsFromValues(Dictionary<string, string> values)
        {
            return string.Join('\n', values
                .Select(keyPair =>
                    keyPair.Key + "=" + keyPair.Value));
        }

        private static string SanitizeFileContentsFromConfiguration(string? contents)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            return contents.Replace("\\n", "\n", StringComparison.InvariantCulture);
        }
    }
}
