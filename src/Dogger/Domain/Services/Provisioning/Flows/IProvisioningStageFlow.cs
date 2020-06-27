using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public interface IProvisioningStageFlow
    {
        IProvisioningStage GetInitialState(IProvisioningStateFactory stateFactory);
        IProvisioningStage? GetNextState(
            IProvisioningStage currentStage,
            IProvisioningStateFactory stateFactory);
    }

}
