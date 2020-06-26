using System;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public class ProvisioningStateFactory : IProvisioningStateFactory
    {
        private readonly IServiceProvider serviceProvider;

        public ProvisioningStateFactory(
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
