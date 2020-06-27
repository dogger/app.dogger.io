using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class ProvisionInstanceStageFlow : IProvisioningStageFlow
    {
        private readonly string planId;
        private readonly Models.Instance databaseInstance;

        public Guid? UserId { get; set; }

        public string PlanId => this.planId;
        public Models.Instance DatabaseInstance => this.databaseInstance;

        public ProvisionInstanceStageFlow(
            string planId,
            Models.Instance databaseInstance)
        {
            this.planId = planId;
            this.databaseInstance = databaseInstance;
        }

        public IProvisioningStage GetInitialState(
            IProvisioningStateFactory stateFactory)
        {
            return stateFactory.Create<CreateLightsailInstanceStage>(state =>
            {
                state.DatabaseInstance = this.databaseInstance;
                state.PlanId = this.planId;
            });
        }

        public IProvisioningStage? GetNextState(
            IProvisioningStage currentStage,
            IProvisioningStateFactory stateFactory)
        {
            switch (currentStage)
            {
                case ICreateLightsailInstanceStage _:
                    return stateFactory.Create<InstallSoftwareOnInstanceStage>();

                case IInstallSoftwareOnInstanceStage _:
                    return null;

                default:
                    throw new UnknownFlowStageException($"Could not determine the next state for a state of type {currentStage.GetType().FullName}.");
            }
        }
    }
}
