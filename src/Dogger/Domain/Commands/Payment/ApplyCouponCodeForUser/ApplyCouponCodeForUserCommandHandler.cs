using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Queries.Payment.GetCouponByCode;
using Dogger.Domain.Queries.Payment.GetCouponById;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser
{
    public class ApplyCouponCodeForUserCommandHandler : IRequestHandler<ApplyCouponCodeForUserCommand, bool>
    {
        private readonly IMediator mediator;
        private readonly CustomerService? stripeCustomerService;

        public ApplyCouponCodeForUserCommandHandler(
            IMediator mediator,
            IOptionalService<CustomerService> stripeCustomerService)
        {
            this.mediator = mediator;
            this.stripeCustomerService = stripeCustomerService.Value;
        }

        public async Task<bool> Handle(
            ApplyCouponCodeForUserCommand request,
            CancellationToken cancellationToken)
        {
            if (this.stripeCustomerService == null)
                return false;

            var promotionCode = await this.mediator.Send(
                new GetCouponByCodeQuery(request.CouponCode),
                cancellationToken);
            if (promotionCode == null)
                return false;

            await this.stripeCustomerService.UpdateAsync(
                request.User.StripeCustomerId,
                new CustomerUpdateOptions()
                {
                    Coupon = promotionCode.Coupon.Id
                },
                default,
                cancellationToken);

            return true;
        }
    }
}

