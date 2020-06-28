using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPlanById
{
    public class GetPlanByIdQuery : IRequest<Plan?>
    {
        public string Id { get; }

        public GetPlanByIdQuery(
            string id)
        {
            Id = id;
        }
    }
}
