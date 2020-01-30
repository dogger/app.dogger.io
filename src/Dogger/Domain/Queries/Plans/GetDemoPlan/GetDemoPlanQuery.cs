using Amazon.Lightsail.Model;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetDemoPlan
{
    public class GetDemoPlanQuery : IRequest<Plan>
    {
    }
}
