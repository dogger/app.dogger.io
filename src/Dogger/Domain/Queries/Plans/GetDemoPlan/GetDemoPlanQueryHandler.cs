using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetDemoPlan
{
    public class GetDemoPlanQueryHandler : IRequestHandler<GetDemoPlanQuery, Plan>
    {
        private readonly IMediator mediator;

        public GetDemoPlanQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<Plan> Handle(GetDemoPlanQuery request, CancellationToken cancellationToken)
        {
            var allPlans = await mediator.Send(
                new GetSupportedPlansQuery(),
                cancellationToken);

            return allPlans
                .OrderBy(x => x.PriceInHundreds)
                .First(x => x.Bundle.RamSizeInGb >= 4);
        }
    }
}
