using Dogger.Domain.Services.Provisioning.States;
using MediatR;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class InitialStateContext
    {
        public InitialStateContext(
            IMediator mediator, 
            IProvisioningStateFactory stateFactory)
        {
            this.Mediator = mediator;
            this.StateFactory = stateFactory;
        }

        public IProvisioningStateFactory StateFactory { get; }
        public IMediator Mediator { get; }
    }
}
