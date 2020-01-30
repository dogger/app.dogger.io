using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;

namespace Dogger.Domain.Services.Provisioning
{
    [ExcludeFromCodeCoverage]
    public class PreCompletedProvisioningJob : IProvisioningJob
    {
        public bool IsEnded => true;
        public bool IsSucceeded => true;
        public bool IsFailed => false;

        public string Id => ProvisioningService.CompletedJobId;

        public StateUpdateException? Exception
        {
            get => null;
            set => throw new InvalidOperationException("Can't set exception on a pre-completed provisioning job.");
        }

        public IProvisioningState CurrentState
        {
            get => new CompletedProvisioningState();
            set => throw new InvalidOperationException("Can't set state on a pre-completed provisioning job.");
        }

        private class CompletedProvisioningState : IProvisioningState
        {
            public string Description => string.Empty;

            public Task<ProvisioningStateUpdateResult> UpdateAsync()
            {
                return Task.FromResult(ProvisioningStateUpdateResult.Succeeded);
            }

            public Task InitializeAsync()
            {
                return Task.CompletedTask;
            }

            public void Dispose()
            {
            }
        }
    }
}
