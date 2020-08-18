using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponById
{
    public class GetCouponByIdQueryHandler : IRequestHandler<GetCouponByIdQuery, PromotionCode?>
    {
        private readonly PromotionCodeService? stripePromotionCodeService;

        public GetCouponByIdQueryHandler(
            IOptionalService<PromotionCodeService> stripePromotionCodeService)
        {
            this.stripePromotionCodeService = stripePromotionCodeService.Value;
        }

        public async Task<PromotionCode?> Handle(
            GetCouponByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (this.stripePromotionCodeService == null)
                return null;

            var promotionCodes = await this.stripePromotionCodeService.ListAsync(
                new PromotionCodeListOptions()
                {
                    Coupon = request.Id
                },
                default,
                cancellationToken);
            return promotionCodes.Data.SingleOrDefault();
        }
    }
}

