using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public interface IProvisioningStateFlow
    {
        Task<IProvisioningState> GetInitialStateAsync(InitialStateContext context);
        Task<IProvisioningState?> GetNextStateAsync(NextStateContext context);
    }

}
