﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetCouponById;
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

            var promotionCodes = await this.stripePromotionCodeService.ListAsync(
                new PromotionCodeListOptions()
                {
                    Code = request.Code
                },
                default,
                cancellationToken);
            return promotionCodes.Data.SingleOrDefault();
        }
    }
}

