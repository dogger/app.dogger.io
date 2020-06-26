using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class AggregateProvisioningStageFlow : IProvisioningStageFlow
    {
        private readonly IList<IProvisioningStageFlow> flows;

        private readonly IProvisioningStageFlow lastFlow;
        private readonly IProvisioningStageFlow firstFlow;

        private IProvisioningStageFlow currentFlow;

        public IProvisioningStageFlow[] Flows => this.flows.ToArray();

        public AggregateProvisioningStageFlow(
            params IProvisioningStageFlow[] flows)
        {
            this.flows = flows.ToList();
            this.currentFlow = this.firstFlow = flows.FirstOrDefault();
            this.lastFlow = flows.LastOrDefault();
        }

        public async Task<IProvisioningStage> GetInitialStateAsync(InitialStateContext context)
        {
            if (this.firstFlow != this.currentFlow)
                throw new InvalidOperationException("The first flow is not the active flow, so the initial state can't be fetched.");

            return await this.firstFlow.GetInitialStateAsync(context);
        }

        public async Task<IProvisioningStage?> GetNextStateAsync(NextStageContext context)
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

        public TFlow GetFlowOfType<TFlow>(int index) where TFlow : IProvisioningStageFlow
        {
            return (TFlow)this.flows[index];
        }
    }
}
