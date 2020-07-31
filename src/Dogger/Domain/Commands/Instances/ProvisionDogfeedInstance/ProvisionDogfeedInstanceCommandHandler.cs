using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Clusters.EnsureClusterWithId;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Arguments;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.AspNet.Options.Dogfeed;
using Dogger.Infrastructure.IO;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Instance = Dogger.Domain.Models.Instance;

namespace Dogger.Domain.Commands.Instances.ProvisionDogfeedInstance
{
    public class ProvisionDogfeedInstanceCommandHandler : IRequestHandler<ProvisionDogfeedInstanceCommand, IProvisioningJob>
    {
        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;
        private readonly IConfiguration configuration;
        private readonly IFile file;

        private readonly IOptionsMonitor<DogfeedOptions> dogfeedOptionsMonitor;

        private readonly DataContext dataContext;

        public ProvisionDogfeedInstanceCommandHandler(
            IProvisioningService provisioningService,
            IMediator mediator,
            IConfiguration configuration,
            IOptionsMonitor<DogfeedOptions> dogfeedOptionsMonitor,
            IFile file,
            DataContext dataContext)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.file = file;
            this.dataContext = dataContext;
            this.configuration = configuration;
            this.dogfeedOptionsMonitor = dogfeedOptionsMonitor;
        }

        public async Task<IProvisioningJob> Handle(ProvisionDogfeedInstanceCommand request, CancellationToken cancellationToken)
        {
            var dogfeedOptions = this.dogfeedOptionsMonitor.CurrentValue;
            if (dogfeedOptions.Files == null)
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
                PlanId = firstCapablePlan.Id
            };

            cluster.Instances.Add(instance);
            await this.dataContext.Instances.AddAsync(instance, cancellationToken);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            var dockerFiles = await GetDockerFilesAsync(dogfeedOptions);

            return await this.provisioningService.ScheduleJobAsync(
                new AggregateProvisioningStateFlow(
                    new ProvisionInstanceStateFlow(
                        firstCapablePlan.Id,
                        instance),
                    new DeployToClusterStateFlow(
                        request.InstanceName,
                        SanitizeDockerComposeYmlFilePaths(dogfeedOptions.Files))
                    {
                        Files = dockerFiles,
                        Authentication = new[] {
                            new DockerAuthenticationArguments(
                                username: dockerHubOptions.Username,
                                password: dockerHubOptions.Password)
                            {
                                RegistryHostName = "docker.pkg.github.com"
                            }
                        }
                    }));
        }

        private static string[] SanitizeDockerComposeYmlFilePaths(
            string[] dogfeedOptionsDockerComposeYmlFilePaths)
        {
            return dogfeedOptionsDockerComposeYmlFilePaths
                .Select(SanitizeDockerComposeYmlFilePath)
                .ToArray();
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
        private async Task<InstanceDockerFile[]> GetDockerFilesAsync(DogfeedOptions options)
        {
            var elasticsearchOptions = options.Elasticsearch;
            if (elasticsearchOptions == null)
                throw new InvalidOperationException("Could not find Elasticsearch options.");

            if (options.Files == null)
                throw new InvalidOperationException("Could not find Docker Compose YML contents.");

            var instanceEnvironmentVariableFile = GetInstanceEnvironmentVariableFile(configuration);

            var elasticsearchInstancePassword =
                elasticsearchOptions.InstancePassword ??
                throw new InvalidOperationException("No Elasticsearch instance password was specified.");

            var files = new List<InstanceDockerFile>()
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
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.AdminKeyContents)),
                new InstanceDockerFile(
                    "certs/admin.pem",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.AdminPemContents)),
                new InstanceDockerFile(
                    "certs/node.key",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.NodeKeyContents)),
                new InstanceDockerFile(
                    "certs/node.pem",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.NodePemContents)),
                new InstanceDockerFile(
                    "certs/root-ca.key",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.RootCaKeyContents)),
                new InstanceDockerFile(
                    "certs/root-ca.pem",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.RootCaPemContents)),
                new InstanceDockerFile(
                    "config/elasticsearch.yml",
                    SanitizeFileContentsFromConfigurationAsBytes(elasticsearchOptions.ConfigurationYmlContents))
            };

            foreach (var ymlFilePath in options.Files)
            {
                files.Add(new InstanceDockerFile(
                    SanitizeDockerComposeYmlFilePath(ymlFilePath),
                    await file.ReadAllBytesAsync(ymlFilePath)));
            }

            return files.ToArray();
        }

        private static string SanitizeDockerComposeYmlFilePath(string ymlFilePath)
        {
            return Path.GetFileName(ymlFilePath);
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

        private static byte[] FormatEnvironmentVariableFileContentsFromValues(Dictionary<string, string> values)
        {
            return Encoding.UTF8.GetBytes(
                string.Join('\n', values
                    .Select(keyPair =>
                        keyPair.Key + "=" + keyPair.Value)));
        }

        private static string SanitizeFileContentsFromConfigurationAsString(string? contents)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            return contents.Replace("\\n", "\n", StringComparison.InvariantCulture);
        }

        private static byte[] SanitizeFileContentsFromConfigurationAsBytes(string? contents)
        {
            var text = SanitizeFileContentsFromConfigurationAsString(contents);
            if (text == null)
                return Array.Empty<byte>();

            return Encoding.UTF8.GetBytes(text);
        }
    }
}
