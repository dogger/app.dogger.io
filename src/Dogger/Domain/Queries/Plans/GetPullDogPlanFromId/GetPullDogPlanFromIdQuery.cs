using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetPullDogPlanFromId
{
    public class GetPullDogPlanFromIdQuery : IRequest<PullDogPlan>
    {
        public string Id { get; }

        public GetPullDogPlanFromIdQuery(string id)
        {
            this.Id = id;
        }
    }
}
