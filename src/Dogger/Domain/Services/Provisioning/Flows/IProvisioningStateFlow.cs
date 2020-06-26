using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public interface IProvisioningStateFlow
    {
        Task<IProvisioningStage> GetInitialStateAsync(InitialStateContext context);
        Task<IProvisioningStage?> GetNextStateAsync(NextStateContext context);
    }

}
