using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetSupportedPlans
{

    public class GetSupportedPlansQuery : IRequest<IReadOnlyCollection<Plan>>
    {
    }
}
