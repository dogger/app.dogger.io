using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioned;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using MediatR;

namespace Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioning
{
    public class RegisterInstanceAsProvisioningCommandHandler : IRequestHandler<RegisterInstanceAsProvisioningCommand>
    {
        private readonly DataContext dataContext;

        private readonly IMediator mediator;

        [DebuggerStepThrough]
        public RegisterInstanceAsProvisioningCommandHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(
            RegisterInstanceAsProvisioningCommand request, 
            CancellationToken cancellationToken)
        {
            var instance = await this.mediator.Send(
                new GetInstanceByNameQuery(request.InstanceName),
                cancellationToken);
            if (instance == null)
                throw new InstanceNotFoundException();

            instance.IsProvisioned = false;
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
