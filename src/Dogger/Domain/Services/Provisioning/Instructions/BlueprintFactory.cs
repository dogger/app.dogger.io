using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance;
using Dogger.Domain.Services.Provisioning.Stages.RunDockerComposeOnInstance;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public class BlueprintFactory : IBlueprintFactory
    {
        private readonly IBlueprintBuilderFactory blueprintBuilderFactory;
        private readonly IProvisioningStageFactory stageFactory;

        public BlueprintFactory(
            IBlueprintBuilderFactory blueprintBuilderFactory,
            IProvisioningStageFactory stageFactory)
        {
            this.blueprintBuilderFactory = blueprintBuilderFactory;
            this.stageFactory = stageFactory;
        }

        public Blueprint Create(
            string planId,
            Instance instance)
        {
            var builder = this.blueprintBuilderFactory.Create();

            this.stageFactory
                .Create<CreateLightsailInstanceStage>(stage =>
                {
                    stage.DatabaseInstance = instance;
                    stage.PlanId = planId;
                })
                .AddInstructionsTo(builder);

            this.stageFactory
                .Create<InstallSoftwareOnInstanceStage>()
                .AddInstructionsTo(builder);

            this.stageFactory
                .Create<RunDockerComposeOnInstanceStage>()
                .AddInstructionsTo(builder);

            return builder.Build();
        }
    }
}
