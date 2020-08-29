using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetCouponById;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponForUser
{
    public class GetCouponForUserQueryHandler : IRequestHandler<GetCouponForUserQuery, PromotionCode?>
    {
        private readonly IMediator mediator;

        private readonly CustomerService? stripeCustomerService;

        public GetCouponForUserQueryHandler(
            IMediator mediator,
            IOptionalService<CustomerService> stripeCustomerService)
        {
            this.mediator = mediator;
            this.stripeCustomerService = stripeCustomerService.Value;
        }

        public async Task<PromotionCode?> Handle(
            GetCouponForUserQuery request,
            CancellationToken cancellationToken)
        {
            if (this.stripeCustomerService == null)
                return null;

            var customer = await this.stripeCustomerService.GetAsync(
                request.User.StripeCustomerId,
                new CustomerGetOptions()
                {
                    Expand = new List<string>()
                    {
                        "discount"
                    }
                },
                default,
                cancellationToken);
            if(customer == null)
                throw new InvalidOperationException("Stripe customer not found.");

            var promotionCode = customer.Discount.PromotionCodeId;
            if (promotionCode == null)
                return null;

            return await this.mediator.Send(
                new GetCouponByIdQuery(promotionCode),
                cancellationToken);
        }
    }
}

