using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser
{

    public class SetActivePaymentMethodForUserCommandHandler : IRequestHandler<SetActivePaymentMethodForUserCommand>
    {
        private readonly PaymentMethodService? paymentMethodService;
        private readonly CustomerService? customerService;

        [DebuggerStepThrough]
        public SetActivePaymentMethodForUserCommandHandler(
            IOptionalService<PaymentMethodService> stripePaymentMethodService,
            IOptionalService<CustomerService> customerService)
        {
            this.paymentMethodService = stripePaymentMethodService.Value;
            this.customerService = customerService.Value;
        }

        public async Task<Unit> Handle(SetActivePaymentMethodForUserCommand request, CancellationToken cancellationToken)
        {
            if (this.customerService == null || this.paymentMethodService == null)
                return Unit.Value;

            var user = request.User;

            try
            {
                var existingPaymentMethods = await this.paymentMethodService
                    .ListAutoPagingAsync(
                        new PaymentMethodListOptions()
                        {
                            Customer = user.StripeCustomerId,
                            Type = "card"
                        },
                        cancellationToken: cancellationToken)
                    .ToListAsync(cancellationToken);

                await this.paymentMethodService.AttachAsync(
                    request.PaymentMethodId,
                    new PaymentMethodAttachOptions()
                    {
                        Customer = user.StripeCustomerId
                    }, cancellationToken: cancellationToken);

                await this.customerService.UpdateAsync(user.StripeCustomerId, new CustomerUpdateOptions()
                {
                    InvoiceSettings = new CustomerInvoiceSettingsOptions()
                    {
                        DefaultPaymentMethod = request.PaymentMethodId
                    }
                }, cancellationToken: cancellationToken);

                foreach (var oldPaymentMethod in existingPaymentMethods)
                {
                    await this.paymentMethodService.DetachAsync(
                        oldPaymentMethod.Id,
                        cancellationToken: cancellationToken);
                }
            }
            catch (StripeException ex) when (ex.StripeError.Param == "customer" && ex.StripeError.Code == "resource_missing")
            {
                throw new NoStripeCustomerIdException();
            }

            return Unit.Value;
        }
    }
}
