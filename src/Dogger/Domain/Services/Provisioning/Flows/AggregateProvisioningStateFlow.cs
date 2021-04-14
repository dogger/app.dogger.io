using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.States;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class AggregateProvisioningStateFlow : IProvisioningStateFlow
    {
        private readonly IList<IProvisioningStateFlow> flows;

        private readonly IProvisioningStateFlow lastFlow;
        private readonly IProvisioningStateFlow firstFlow;

        private IProvisioningStateFlow currentFlow;

        public IProvisioningStateFlow[] Flows => this.flows.ToArray();

        public AggregateProvisioningStateFlow(
            params IProvisioningStateFlow[] flows)
        {
            this.flows = flows.ToList();
            this.currentFlow = this.firstFlow = flows.First();
            this.lastFlow = flows.Last();
        }

        public async Task<IProvisioningState> GetInitialStateAsync(InitialStateContext context)
        {
            if (this.firstFlow != this.currentFlow)
                throw new InvalidOperationException("The first flow is not the active flow, so the initial state can't be fetched.");

            return await this.firstFlow.GetInitialStateAsync(context);
        }

        public async Task<IProvisioningState?> GetNextStateAsync(NextStateContext context)
        {
            var nextState = await this.currentFlow.GetNextStateAsync(context);
            if (nextState != null)
                return nextState;

            if (this.currentFlow == this.lastFlow)
                return null;

            var currentFlowIndex = this.flows.IndexOf(this.currentFlow);
            this.currentFlow = this.flows[currentFlowIndex + 1];

            return await this.currentFlow.GetInitialStateAsync(
                new InitialStateContext(
                    context.Mediator,
                    context.StateFactory));
        }

        public TFlow GetFlowOfType<TFlow>(int index) where TFlow : IProvisioningStateFlow
        {
            return (TFlow)this.flows[index];
        }
    }
}
