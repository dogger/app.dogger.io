using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public interface IProvisioningStageFlow
    {
        Task<IProvisioningStage> GetInitialStateAsync(IProvisioningStateFactory stateFactory);
        Task<IProvisioningStage?> GetNextStateAsync(
            IProvisioningStage currentStage,
            IProvisioningStateFactory stateFactory);
    }

}
