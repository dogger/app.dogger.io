using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Stripe;

namespace Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser
{

    public class SetActivePaymentMethodForUserCommandHandler : IRequestHandler<SetActivePaymentMethodForUserCommand>
    {
        private readonly PaymentMethodService stripePaymentMethodService;
        private readonly CustomerService customerService;

        [DebuggerStepThrough]
        public SetActivePaymentMethodForUserCommandHandler(
            PaymentMethodService stripePaymentMethodService,
            CustomerService customerService)
        {
            this.stripePaymentMethodService = stripePaymentMethodService;
            this.customerService = customerService;
        }

        public async Task<Unit> Handle(SetActivePaymentMethodForUserCommand request, CancellationToken cancellationToken)
        {
            var user = request.User;
            if (user.StripeCustomerId == null)
                throw new NoStripeCustomerIdException();

            var existingPaymentMethods = await this.stripePaymentMethodService.ListAsync(new PaymentMethodListOptions()
            {
                Customer = user.StripeCustomerId,
                Type = "card"
            }, cancellationToken: cancellationToken);

            await this.stripePaymentMethodService.AttachAsync(
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

            foreach(var oldPaymentMethod in existingPaymentMethods)
            {
                await this.stripePaymentMethodService.DetachAsync(
                    oldPaymentMethod.Id,
                    cancellationToken: cancellationToken);
            }

            return Unit.Value;
        }
    }
}
