using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment
{
    public class GetActivePaymentMethodForUserQueryHandler : IRequestHandler<GetActivePaymentMethodForUserQuery, PaymentMethod?>
    {
        private readonly PaymentMethodService? stripePaymentMethodService;

        public GetActivePaymentMethodForUserQueryHandler(
            IOptionalService<PaymentMethodService> stripePaymentMethodService)
        {
            this.stripePaymentMethodService = stripePaymentMethodService.Value;
        }

        public async Task<PaymentMethod?> Handle(GetActivePaymentMethodForUserQuery request, CancellationToken cancellationToken)
        {
            if (this.stripePaymentMethodService == null)
                return null;

            if(request.User.StripeCustomerId == null)
                return null;

            var paymentMethods = await this.stripePaymentMethodService.ListAsync(new PaymentMethodListOptions()
            {
                Customer = request.User.StripeCustomerId,
                Type = "card"
            }, default, cancellationToken);
            return paymentMethods.SingleOrDefault();
        }
    }
}
