using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser
{
    public class SetActivePaymentMethodForUserCommand : IRequest
    {
        public string PaymentMethodId { get; }
        public User User { get; }

        public SetActivePaymentMethodForUserCommand(
            User user,
            string paymentMethodId)
        {
            this.User = user;
            this.PaymentMethodId = paymentMethodId;
        }
    }
}
