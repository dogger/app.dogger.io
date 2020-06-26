using Dogger.Domain.Services.Provisioning.Stages;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class NextStateContext
    {
        public NextStateContext(
            IMediator mediator,
            IProvisioningStateFactory stateFactory, 
            IProvisioningStage currentStage)
        {
            this.Mediator = mediator;
            this.StateFactory = stateFactory;
            this.CurrentStage = currentStage;
        }

        public IProvisioningStateFactory StateFactory { get; }
        public IMediator Mediator { get; }
        public IProvisioningStage CurrentStage { get; }
    }
}
