using System;

namespace Dogger.Domain.Services.Provisioning.States
{
    public interface IProvisioningStateFactory
    {
        TState Create<TState>(Action<TState>? modifications = null) where TState : IProvisioningState;
    }
}
