using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings
{
    public class GetPullDogPlanFromSettingsQueryHandler : IRequestHandler<GetPullDogPlanFromSettingsQuery, PullDogPlan?>
    {
        private readonly IMediator mediator;

        public GetPullDogPlanFromSettingsQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<PullDogPlan?> Handle(GetPullDogPlanFromSettingsQuery request, CancellationToken cancellationToken)
        {
            var plan = await mediator.Send(
                new GetPlanByIdQuery(request.DoggerPlanId), 
                cancellationToken);

            return plan?
                .PullDogPlans
                .SingleOrDefault(x => x.PoolSize == request.PoolSize);
        }
    }
}
