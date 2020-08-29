using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponById
{
    public class GetCouponByIdQuery : IRequest<PromotionCode?>
    {
        public string Id { get; }

        public GetCouponByIdQuery(
            string id)
        {
            this.Id = id;
        }
    }
}
