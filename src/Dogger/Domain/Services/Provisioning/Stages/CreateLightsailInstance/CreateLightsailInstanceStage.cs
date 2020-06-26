using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.OpenFirewallPorts;
using Dogger.Domain.Events.ServerProvisioningStarted;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Instances.GetNecessaryInstanceFirewallPorts;
using Dogger.Domain.Services.Amazon.Lightsail;
using MediatR;
using Serilog;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance
{

    public class CreateLightsailInstanceStage : ICreateLightsailInstanceStage
    {
        public string? PlanId { get; set; }

        public Models.Instance? DatabaseInstance { get; set; }

        public async Task<ProvisioningStateUpdateResult> UpdateAsync()
        {
            if (this.PlanId == null)
                throw new InvalidOperationException("Plan ID is not set.");

            if (this.DatabaseInstance == null)
                throw new InvalidOperationException("Database instance is not set.");

            if (this.currentOperationIds == null)
                throw new InvalidOperationException("State has not been initialized yet.");

            var operations = await this.lightsailOperationService.GetOperationsFromIdsAsync(
                this.currentOperationIds);

            var failedOperation = operations.FirstOrDefault(x => x.Status == OperationStatus.Failed);
            if (failedOperation != null)
            {
                this.logger.Error("Got error code {ErrorCode} while trying to provision instance.", failedOperation.ErrorCode);
                this.Description = "Could not provision instance.";

                throw new StageUpdateException($"Got error code {failedOperation.ErrorCode} while trying to provision instance.");
            }

            if (operations.All(x => x.Status == OperationStatus.NotStarted))
            {
                this.Description = "Waiting for response from AWS";
                return ProvisioningStateUpdateResult.InProgress;
            }

            if (operations.Any(x => x.Status == OperationStatus.Started))
            {
                this.Description = "AWS has started the provisioning process";
                return ProvisioningStateUpdateResult.InProgress;
            }

            if (operations.All(x => x.Status == OperationStatus.Succeeded))
            {
                return await HandleSucceededStateAsync();
            }

            throw new InvalidOperationException($"Unknown operation status situation.");
        }

        public async Task InitializeAsync()
        {
            if (this.DatabaseInstance == null)
                throw new InvalidOperationException("Database instance not set.");

            if (this.DatabaseInstance.Cluster == null)
                throw new InvalidOperationException("Database instance's cluster not set.");

            var response = await this.amazonLightsailClient.CreateInstancesAsync(new CreateInstancesRequest()
            {
                BundleId = this.PlanId,
                KeyPairName = "dogger-2020-03-24",
                InstanceNames = new[]
                {
                    this.DatabaseInstance.Name
                }.ToList(),
                BlueprintId = "ubuntu_18_04/1",
                AvailabilityZone = "eu-west-1a",
                Tags = new List<Tag>()
                {
                    new Tag()
                    {
                        Key = "UserId",
                        Value = this.DatabaseInstance.Cluster.UserId.ToString() ?? string.Empty
                    },
                    new Tag()
                    {
                        Key = "StripePlanId",
                        Value = this.PlanId
                    },
                    new Tag()
                    {
                        Key = "ClusterId",
                        Value = this.DatabaseInstance.Cluster.Id.ToString()
                    },
                    new Tag()
                    {
                        Key = "InstanceId",
                        Value = this.DatabaseInstance.Id.ToString()
                    }
                }
            });

            this.currentOperationIds = response
                .Operations
                .Select(x => x.Id)
                .ToArray();

            await this.mediator.Send(new ServerProvisioningStartedEvent(this.DatabaseInstance));
        }

        private async Task<ProvisioningStateUpdateResult> HandleSucceededStateAsync()
        {
            if (this.DatabaseInstance?.Name == null)
                throw new InvalidOperationException("No instance name was found.");

            var instance = await this.mediator.Send(
                new GetLightsailInstanceByNameQuery(this.DatabaseInstance.Name));
            if (instance == null)
                throw new InvalidOperationException("Instance was not found.");

            if (instance.State.Name != "running")
            {
                this.Description = "Waiting for instance to start";
                return ProvisioningStateUpdateResult.InProgress;
            }

            await OpenNecessaryFirewallPortsOnInstanceAsync();

            this.CreatedLightsailInstance = instance;

            return ProvisioningStateUpdateResult.Succeeded;
        }

        private async Task OpenNecessaryFirewallPortsOnInstanceAsync()
        {
            if (this.DatabaseInstance?.Name == null)
                throw new InvalidOperationException("No instance name was found.");

            this.Description = "Opening firewall for exposed ports and protocols";

            var portsToOpen = await this.mediator.Send(new GetNecessaryInstanceFirewallPortsQuery(this.DatabaseInstance.Name));

            await this.mediator.Send(new OpenFirewallPortsCommand(
                this.DatabaseInstance.Name,
                portsToOpen));
        }

        public void Dispose()
        {
        }
    }
}
