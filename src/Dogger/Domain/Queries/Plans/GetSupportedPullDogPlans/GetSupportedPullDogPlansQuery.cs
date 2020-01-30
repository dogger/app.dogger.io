using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetSupportedPullDogPlans
{
    public class GetSupportedPullDogPlansQuery : IRequest<ICollection<PullDogPlan>>
    {
    }
}
