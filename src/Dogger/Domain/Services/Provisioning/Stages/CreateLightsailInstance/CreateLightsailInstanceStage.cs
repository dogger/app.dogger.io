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
using Dogger.Domain.Services.Provisioning.Instructions;
using MediatR;
using Serilog;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance
{

    public class CreateLightsailInstanceStage : ICreateLightsailInstanceStage
    {
        private readonly IAmazonLightsailInstructionFactory amazonLightsailInstructionFactory;

        public string? PlanId { get; set; }

        public Models.Instance? DatabaseInstance { get; set; }

        public CreateLightsailInstanceStage(
            IAmazonLightsailInstructionFactory amazonLightsailInstructionFactory)
        {
            this.amazonLightsailInstructionFactory = amazonLightsailInstructionFactory;
        }

        public void CollectInstructions(IInstructionGroupCollector instructionCollector)
        {
            if (this.PlanId == null)
                throw new InvalidOperationException("Plan ID is not set.");

            if (this.DatabaseInstance == null)
                throw new InvalidOperationException("Database instance is not set.");

            using var createInstanceGroup = instructionCollector.CollectGroup("Creating AWS Lightsail instance");
            createInstanceGroup.CollectInstructionWithSignal(
                "create-instance",
                this.amazonLightsailInstructionFactory.Create(
                    new CreateInstancesRequest()
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
                    }));
        }
    }
}
