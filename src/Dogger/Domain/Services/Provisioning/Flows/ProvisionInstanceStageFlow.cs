using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.CompleteInstanceSetup;
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

        public async Task<IProvisioningStage> GetInitialStateAsync(
            InitialStateContext context)
        {
            return context.StateFactory.Create<CreateLightsailInstanceStage>(state =>
            {
                state.DatabaseInstance = this.databaseInstance;
                state.PlanId = this.planId;
            });
        }

        public async Task<IProvisioningStage?> GetNextStateAsync(
            NextStageContext context)
        {
            switch (context.CurrentStage)
            {
                case ICreateLightsailInstanceStage createLightsailInstanceState:
                    return TransitionFromCreateToInstall(context.StateFactory, createLightsailInstanceState);

                case IInstallSoftwareOnInstanceStage installDockerOnInstanceState:
                    return TransitionFromInstallToComplete(context.StateFactory, installDockerOnInstanceState);

                case ICompleteInstanceSetupStage _:
                    return null;

                default:
                    throw new UnknownFlowStageException($"Could not determine the next state for a state of type {context.CurrentStage.GetType().FullName}.");
            }
        }

        private IProvisioningStage? TransitionFromInstallToComplete(
            IProvisioningStateFactory stateFactory, 
            IInstallSoftwareOnInstanceStage installSoftwareOnInstanceStage)
        {
            return stateFactory.Create<CompleteInstanceSetupStage>(state =>
            {
                state.IpAddress = installSoftwareOnInstanceStage.IpAddress;

                state.UserId = UserId;
                state.InstanceName = DatabaseInstance.Name;
            });
        }

        private IProvisioningStage TransitionFromCreateToInstall(
            IProvisioningStateFactory stateFactory, 
            ICreateLightsailInstanceStage createLightsailInstanceStage)
        {
            return stateFactory.Create<InstallSoftwareOnInstanceStage>(state =>
            {
                state.IpAddress = createLightsailInstanceStage.CreatedLightsailInstance.PublicIpAddress;

                state.InstanceName = DatabaseInstance.Name;
                state.UserId = UserId;
            });
        }
    }
}
