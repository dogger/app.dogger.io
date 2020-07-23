using System;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.ChangePullDogPlan
{
    public class ChangePullDogPlanCommandHandler : IRequestHandler<ChangePullDogPlanCommand>
    {
        private readonly DataContext dataContext;
        private readonly IMediator mediator;

        public ChangePullDogPlanCommandHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(ChangePullDogPlanCommand request, CancellationToken cancellationToken)
        {
            var settings = await this.dataContext
                .PullDogSettings
                .SingleOrDefaultAsync(
                    x => x.UserId == request.UserId,
                    cancellationToken);
            if (settings == null)
                throw new InvalidOperationException("Pull Dog has not been activated for the given user.");

            if (request.PoolSize > 0)
            {
                var matchingPullDogPlan = await this.mediator.Send(
                    new GetPullDogPlanFromSettingsQuery(
                        request.PlanId,
                        request.PoolSize),
                    cancellationToken);
                if (matchingPullDogPlan == null)
                    throw new InvalidOperationException("The given plan is invalid.");

                settings.PoolSize = request.PoolSize;
                settings.PlanId = request.PlanId;
            }
            else
            {
                var demoPlan = await this.mediator.Send(
                    new GetDemoPlanQuery(),
                    cancellationToken);

                settings.PoolSize = 0;
                settings.PlanId = demoPlan.Id;
            }

            await this.dataContext.SaveChangesAsync(cancellationToken);

            await this.mediator.Send(
                new UpdateUserSubscriptionCommand(request.UserId),
                cancellationToken);

            await this.mediator.Send(
                new DeleteAllPullDogInstancesForUserCommand(
                    request.UserId,
                    InitiatorType.User),
                cancellationToken);

            return Unit.Value;
        }
    }
}
