using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public class ProvisioningStageFactory : IProvisioningStageFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ProvisioningStageFactory(
            IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public TState Create<TState>(Action<TState>? modifications = null) 
            where TState : IProvisioningStage
        {
            var state = this.serviceProvider.GetRequiredService<TState>();
            modifications?.Invoke(state);

            return state;
        }
    }
}
