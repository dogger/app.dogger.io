using System;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public interface IProvisioningStageFactory
    {
        TState Create<TState>(Action<TState>? modifications = null) where TState : IProvisioningStage;
    }
}
