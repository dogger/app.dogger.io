using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Instances.GetInstanceByName;
using Dogger.Domain.Queries.Users.GetUserById;
using Dogger.Infrastructure.Time;
using MediatR;
using Polly;
using Stripe;

namespace Dogger.Domain.Commands.Instances.RegisterInstanceAsProvisioned
{
    public class RegisterInstanceAsProvisionedCommandHandler : IRequestHandler<RegisterInstanceAsProvisionedCommand>
    {
        private readonly DataContext dataContext;

        private readonly IMediator mediator;

        [DebuggerStepThrough]
        public RegisterInstanceAsProvisionedCommandHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(
            RegisterInstanceAsProvisionedCommand request, 
            CancellationToken cancellationToken)
        {
            var instance = await this.mediator.Send(
                new GetInstanceByNameQuery(request.InstanceName),
                cancellationToken);
            if (instance == null)
                throw new InstanceNotFoundException();

            instance.IsProvisioned = true;
            await this.dataContext.SaveChangesAsync(cancellationToken);

            if (request.UserId != null)
            {
                await this.mediator.Send(
                    new UpdateUserSubscriptionCommand(request.UserId.Value),
                    cancellationToken);
            }

            return Unit.Value;
        }
    }
}
