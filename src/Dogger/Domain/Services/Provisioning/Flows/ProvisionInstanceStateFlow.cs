using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.States.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class ProvisionInstanceStateFlow : IProvisioningStateFlow
    {
        private readonly string planId;
        private readonly Models.Instance databaseInstance;

        public Guid? UserId { get; set; }

        public string PlanId => this.planId;
        public Models.Instance DatabaseInstance => this.databaseInstance;

        public ProvisionInstanceStateFlow(
            string planId,
            Models.Instance databaseInstance)
        {
            this.planId = planId;
            this.databaseInstance = databaseInstance;
        }

        public async Task<IProvisioningState> GetInitialStateAsync(
            InitialStateContext context)
        {
            return context.StateFactory.Create<CreateLightsailInstanceState>(state =>
            {
                state.DatabaseInstance = this.databaseInstance;
                state.PlanId = this.planId;
            });
        }

        public async Task<IProvisioningState?> GetNextStateAsync(
            NextStateContext context)
        {
            return context.CurrentState switch
            {
                ICreateLightsailInstanceState createLightsailInstanceState =>
                    TransitionFromCreateToInstall(context.StateFactory, createLightsailInstanceState),

                IInstallSoftwareOnInstanceState installDockerOnInstanceState =>
                    TransitionFromInstallToComplete(context.StateFactory, installDockerOnInstanceState),

                ICompleteInstanceSetupState _ =>
                    null,

                _ => throw new UnknownFlowStateException($"Could not determine the next state for a state of type {context.CurrentState.GetType().FullName}."),
            };
        }

        private IProvisioningState? TransitionFromInstallToComplete(
            IProvisioningStateFactory stateFactory,
            IInstallSoftwareOnInstanceState installSoftwareOnInstanceState)
        {
            return stateFactory.Create<CompleteInstanceSetupState>(state =>
            {
                state.IpAddress = installSoftwareOnInstanceState.IpAddress;

                state.UserId = UserId;
                state.InstanceName = DatabaseInstance.Name;
            });
        }

        private IProvisioningState TransitionFromCreateToInstall(
            IProvisioningStateFactory stateFactory,
            ICreateLightsailInstanceState createLightsailInstanceState)
        {
            return stateFactory.Create<InstallSoftwareOnInstanceState>(state =>
            {
                state.IpAddress = createLightsailInstanceState.CreatedLightsailInstance.PublicIpAddress;

                state.InstanceName = DatabaseInstance.Name;
                state.UserId = UserId;
            });
        }
    }
}
