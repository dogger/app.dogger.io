using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponByCode
{
    public class GetCouponByCodeQuery : IRequest<PromotionCode?>
    {
        public string Code { get; }

        public GetCouponByCodeQuery(
            string code)
        {
            this.Code = code;
        }
    }
}
