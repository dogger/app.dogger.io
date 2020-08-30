using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetSubscriptionById
{
    public class GetSubscriptionByIdQuery : IRequest<Subscription?>
    {
        public string Id { get; }

        public GetSubscriptionByIdQuery(string id)
        {
            this.Id = id;
        }
    }
}
