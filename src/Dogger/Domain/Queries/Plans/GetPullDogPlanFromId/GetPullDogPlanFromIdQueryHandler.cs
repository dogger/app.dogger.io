using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPullDogPlanFromId
{
    public class GetPullDogPlanFromIdQueryHandler : IRequestHandler<GetPullDogPlanFromIdQuery, PullDogPlan>
    {
        private readonly IMediator mediator;

        public GetPullDogPlanFromIdQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<PullDogPlan> Handle(
            GetPullDogPlanFromIdQuery request,
            CancellationToken cancellationToken)
        {
            var supportedPlans = await this.mediator.Send(
                new GetSupportedPullDogPlansQuery(),
                cancellationToken);
            return supportedPlans.Single(x => x.Id == request.Id);
        }
    }
}

