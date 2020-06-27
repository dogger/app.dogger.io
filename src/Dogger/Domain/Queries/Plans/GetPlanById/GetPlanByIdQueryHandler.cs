using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPlanById
{
    public class GetPlanByIdQueryHandler : IRequestHandler<GetPlanByIdQuery, Plan?>
    {
        private readonly IMediator mediator;

        public GetPlanByIdQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Plan?> Handle(GetPlanByIdQuery request, CancellationToken cancellationToken)
        {
            var allPlans = await mediator.Send(
                new GetSupportedPlansQuery(),
                cancellationToken);

            return allPlans.SingleOrDefault(x => x.Id == request.Id);
        }
    }
}
