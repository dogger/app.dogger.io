using Dogger.Domain.Models;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment
{
    public class GetActivePaymentMethodForUserQuery : IRequest<PaymentMethod?>
    {
        public User User { get; }

        public GetActivePaymentMethodForUserQuery(
            User user)
        {
            User = user;
        }
    }
}
