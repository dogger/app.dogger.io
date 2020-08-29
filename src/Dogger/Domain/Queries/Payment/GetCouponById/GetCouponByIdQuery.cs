using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponById
{
    public class GetCouponByIdQuery : IRequest<PromotionCode?>
    {
        public string Code { get; }

        public GetCouponByIdQuery(
            string code)
        {
            this.Code = code;
        }
    }
}
