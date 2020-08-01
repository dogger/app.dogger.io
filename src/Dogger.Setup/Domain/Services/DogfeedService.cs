using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.AssignStaticIpToInstance;
using Dogger.Domain.Commands.Amazon.Lightsail.AttachInstancesToLoadBalancer;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllInstances;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLoadBalancerByName;
using Dogger.Domain.Services.Provisioning;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Time;
using Dogger.Setup.Domain.Commands.ProvisionDogfeedInstance;
using MediatR;
using Microsoft.Extensions.Configuration;
using Serilog;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Setup.Domain.Services
{
    public class DogfeedService : IDogfeedService
    {
        private const string instanceName = ProvisioningService.ProtectedResourcePrefix + "instance";
        private const string loadBalancerName = ProvisioningService.ProtectedResourcePrefix + "load-balancer";
        private const string ipName = ProvisioningService.ProtectedResourcePrefix + "ip";

        private readonly IProvisioningService provisioningService;
        private readonly IMediator mediator;
        private readonly ILogger logger;
        private readonly ITime time;

        public DogfeedService(
            IProvisioningService provisioningService,
            IMediator mediator,
            ILogger logger,
            ITime time)
        {
            this.provisioningService = provisioningService;
            this.mediator = mediator;
            this.logger = logger;
            this.time = time;
        }

        public async Task DogfeedAsync()
        {
            this.logger.Information("Provisioning has started.");

            await CleanUpAsync();

            try
            {
                var newInstance = await ProvisionNewDogfeedInstanceAsync();

                try
                {
                    var loadBalancer = await GetMainLoadBalancerAsync();

                    await AttachInstanceToLoadBalancerAsync(
                        newInstance,
                        loadBalancer);

                    await WaitForInstanceToBecomeHealthyAsync(
                        loadBalancer,
                        newInstance);

                    await DestroyOldInstancesAsync(
                        loadBalancer,
                        newInstance);

                    await AssignStaticIpAddressToNewInstanceAsync(newInstance);
                }
                catch
                {
                    await DestroyInstanceByNameAsync(newInstance.Name);
                    throw;
                }
            }
            finally
            {
                await CleanUpAsync();
            }
        }

        private async Task AssignStaticIpAddressToNewInstanceAsync(Instance newInstance)
        {
            await this.mediator.Send(new AssignStaticIpToInstanceCommand(
                newInstance.Name,
                ipName));
        }

        private async Task CleanUpAsync()
        {
            var refreshedLoadBalancer = await GetMainLoadBalancerAsync();
            await DestroyRedundantInstancesAsync(refreshedLoadBalancer);
            await DestroyDetachedInstancesAsync(refreshedLoadBalancer);
        }

        private async Task<LoadBalancer> GetMainLoadBalancerAsync()
        {
            return
                await this.mediator.Send(new GetLoadBalancerByNameQuery(loadBalancerName)) ??
                throw new InvalidOperationException("No load balancer was found.");
        }

        /// <summary>
        /// Deletes all instances that are prefixed "main-" but are not part of a load balancer.
        /// </summary>
        private async Task DestroyDetachedInstancesAsync(LoadBalancer loadBalancer)
        {
            var allInstances = await this.mediator.Send(new GetAllInstancesQuery());
            var detachedInstanceNames = allInstances
                .Where(x => x.Name.StartsWith(ProvisioningService.ProtectedResourcePrefix, StringComparison.InvariantCulture))
                .Where(instance => loadBalancer
                    .InstanceHealthSummary
                    .All(x => x.InstanceName != instance.Name))
                .Select(x => x.Name);

            foreach (var detachedInstanceName in detachedInstanceNames)
            {
                this.logger.Warning("Deleting detached instance {InstanceName}.", detachedInstanceName);
                await DestroyInstanceByNameAsync(detachedInstanceName);
            }
        }

        private async Task AttachInstanceToLoadBalancerAsync(
            Instance instance,
            LoadBalancer loadBalancer)
        {
            await this.mediator.Send(new AttachInstancesToLoadBalancerCommand(
                loadBalancer.Name,
                new[]
                {
                    instance.Name
                }));
        }

        private async Task DestroyInstanceByNameAsync(string name)
        {
            await this.mediator.Send(new DeleteInstanceByNameCommand(name, InitiatorType.System));
        }

        private async Task WaitForInstanceToBecomeHealthyAsync(
            LoadBalancer loadBalancer,
            Instance newInstance)
        {
            bool IsUnhealthy()
            {
                var instanceHealth = loadBalancer
                    .InstanceHealthSummary
                    .SingleOrDefault(x => x.InstanceName == newInstance.Name);
                return instanceHealth?.InstanceHealth != InstanceHealthState.Healthy;
            }

            var stopwatch = this.time.StartStopwatch();
            do
            {
                await this.time.WaitAsync(60000);

                if (stopwatch.Elapsed.TotalMinutes > 30)
                {
                    throw new NewInstanceHealthTimeoutException(
                        $"The newly deployed instance {newInstance.Name} was not healthy after 5 minutes.");
                }

                loadBalancer = await this.mediator.Send(new GetLoadBalancerByNameQuery(loadBalancer.Name)) ??
                    throw new InvalidOperationException("Could not refresh load balancer status.");
            } while (IsUnhealthy());
        }

        private async Task DestroyOldInstancesAsync(
            LoadBalancer loadBalancer,
            Instance newInstance)
        {
            var oldInstanceNames = loadBalancer
                .InstanceHealthSummary
                .Where(x => x.InstanceName != newInstance.Name)
                .Select(x => x.InstanceName);

            foreach (var oldInstanceName in oldInstanceNames)
            {
                this.logger.Warning("Deleting old instance {InstanceName}.", oldInstanceName);
                await DestroyInstanceByNameAsync(oldInstanceName);
            }
        }

        /// <summary>
        /// Removes every healthy instance except one, in case there are multiple healthy instances.
        /// </summary>
        private async Task DestroyRedundantInstancesAsync(LoadBalancer loadBalancer)
        {
            var redundantInstanceNames = loadBalancer
                .InstanceHealthSummary
                .Where(x => x.InstanceHealth == InstanceHealthState.Healthy)
                .Select(x => x.InstanceName)
                .Skip(1);

            foreach (var redundantInstanceName in redundantInstanceNames)
            {
                this.logger.Warning("Deleting redundant instance {InstanceName}.", redundantInstanceName);
                await DestroyInstanceByNameAsync(redundantInstanceName);
            }
        }

        private async Task<Instance> ProvisionNewDogfeedInstanceAsync()
        {
            var newInstanceName = $"{instanceName}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            await this.mediator.Send(new ProvisionDogfeedInstanceCommand(newInstanceName));
            await this.provisioningService.ProcessPendingJobsAsync();

            return await this.mediator.Send(new GetLightsailInstanceByNameQuery(newInstanceName)) ??
                throw new InvalidOperationException("Could not fetch newly created instance.");
        }

        public static void MoveDogfeedPrefixedEnvironmentVariableIntoConfiguration(IConfigurationBuilder configurationBuilder)
        {
            const string dogfeedEnvironmentVariableKeyPrefix = "DOGFEED_";

            var configurationValues = Environment
                .GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Select(x => x.Key.ToString()!)
                .Where(x => x.StartsWith(
                    dogfeedEnvironmentVariableKeyPrefix,
                    StringComparison.InvariantCulture))
                .ToDictionary(
                    x => x
                        .Substring(dogfeedEnvironmentVariableKeyPrefix.Length)
                        .Replace("__", ":", StringComparison.InvariantCulture),
                    Environment.GetEnvironmentVariable);
            configurationBuilder.AddInMemoryCollection(configurationValues);
        }
    }
}
