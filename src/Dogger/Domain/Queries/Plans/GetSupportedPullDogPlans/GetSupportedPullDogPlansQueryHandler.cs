using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans
{
    public class GetSupportedPullDogPlansQueryHandler : IRequestHandler<GetSupportedPullDogPlansQuery, ICollection<PullDogPlan>>
    {
        private readonly IMediator mediator;

        public GetSupportedPullDogPlansQueryHandler(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        public async Task<ICollection<PullDogPlan>> Handle(GetSupportedPullDogPlansQuery request, CancellationToken cancellationToken)
        {
            var allPlans = await this.mediator.Send(new GetSupportedPlansQuery(), cancellationToken);
            return allPlans
                .SelectMany(x => x.PullDogPlans)
                .ToArray();
        }
    }
}
