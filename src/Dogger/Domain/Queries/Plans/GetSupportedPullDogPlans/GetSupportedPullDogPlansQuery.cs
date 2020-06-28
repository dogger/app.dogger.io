using System.Collections.Generic;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans
{
    public class GetSupportedPullDogPlansQuery : IRequest<ICollection<PullDogPlan>>
    {
    }
}
