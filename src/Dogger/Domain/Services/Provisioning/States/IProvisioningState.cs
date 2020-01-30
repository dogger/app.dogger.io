using System;
using System.Threading.Tasks;

namespace Dogger.Domain.Services.Provisioning.States
{
    public interface IProvisioningState : IDisposable
    {
        public string Description
        {
            get;
        }

        public Task<ProvisioningStateUpdateResult> UpdateAsync();
        public Task InitializeAsync();
    }

}
