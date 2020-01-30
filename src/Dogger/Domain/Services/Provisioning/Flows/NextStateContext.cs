using Dogger.Domain.Services.Provisioning.States;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class NextStateContext
    {
        public NextStateContext(
            IMediator mediator,
            IProvisioningStateFactory stateFactory, 
            IProvisioningState currentState)
        {
            this.Mediator = mediator;
            this.StateFactory = stateFactory;
            this.CurrentState = currentState;
        }

        public IProvisioningStateFactory StateFactory { get; }
        public IMediator Mediator { get; }
        public IProvisioningState CurrentState { get; }
    }
}
