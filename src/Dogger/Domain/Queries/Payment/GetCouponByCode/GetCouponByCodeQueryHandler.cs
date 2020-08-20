using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponByCode
{
    public class GetCouponByCodeQueryHandler : IRequestHandler<GetCouponByCodeQuery, PromotionCode?>
    {
        private readonly PromotionCodeService? stripePromotionCodeService;

        public GetCouponByCodeQueryHandler(
            IOptionalService<PromotionCodeService> stripePromotionCodeService)
        {
            this.stripePromotionCodeService = stripePromotionCodeService.Value;
        }

        public async Task<PromotionCode?> Handle(
            GetCouponByCodeQuery request,
            CancellationToken cancellationToken)
        {
            if (this.stripePromotionCodeService == null)
                return null;

            return await this.stripePromotionCodeService
                .ListAutoPagingAsync(
                    new PromotionCodeListOptions()
                    {
                        Code = request.Code,
                        Expand = new List<string>()
                        {
                            "data.coupon.applies_to"
                        }
                    },
                    default,
                    cancellationToken)
                .SingleOrDefaultAsync(
                    x => x.Active, 
                    cancellationToken);
        }
    }
}

