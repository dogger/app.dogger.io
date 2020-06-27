using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Provisioning.Instructions;

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

        public void AddInstructionsTo(IBlueprintBuilder blueprintBuilder)
        {
            if (this.PlanId == null)
                throw new InvalidOperationException("Plan ID is not set.");

            if (this.DatabaseInstance == null)
                throw new InvalidOperationException("Database instance is not set.");

            using var createInstanceGroup = blueprintBuilder.AddGroup("Creating AWS Lightsail instance");
            createInstanceGroup.AddInstructionWithSignal(
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
