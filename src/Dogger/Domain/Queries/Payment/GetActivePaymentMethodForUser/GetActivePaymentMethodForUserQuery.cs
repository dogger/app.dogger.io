using Dogger.Domain.Models;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetActivePaymentMethodForUser
{
    public class GetActivePaymentMethodForUserQuery : IRequest<PaymentMethod?>
    {
        public User User { get; }

        public GetActivePaymentMethodForUserQuery(
            User user)
        {
            this.User = user;
        }
    }
}
