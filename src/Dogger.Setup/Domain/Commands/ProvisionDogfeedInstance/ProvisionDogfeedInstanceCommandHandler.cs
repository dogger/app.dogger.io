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
using Dogger.Infrastructure.IO;
using Dogger.Setup.Infrastructure;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using File = System.IO.File;
using Instance = Dogger.Domain.Models.Instance;

namespace Dogger.Setup.Domain.Commands.ProvisionDogfeedInstance
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

            var cluster = await this.mediator.Send(new EnsureClusterWithIdCommand(DataContext.DoggerClusterId), cancellationToken);

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
            var allPlans = await this.mediator.Send(new GetSupportedPlansQuery());

            var firstCapablePlan = allPlans
                .OrderBy(x => x.PriceInHundreds)
                .First(x => x.Bundle.RamSizeInGb >= 4);
            return firstCapablePlan;
        }

        private static async Task<InstanceDockerFile[]> GetDockerFilesAsync(DogfeedOptions options)
        {
            if (options.Files == null)
                throw new InvalidOperationException("Could not find Docker Compose YML contents.");

            return await Task.WhenAll(options
                .Files
                .Select(async path => new InstanceDockerFile(
                    path,
                    await File.ReadAllBytesAsync(path))));
        }

        private static string SanitizeDockerComposeYmlFilePath(string ymlFilePath)
        {
            return Path.GetFileName(ymlFilePath);
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
