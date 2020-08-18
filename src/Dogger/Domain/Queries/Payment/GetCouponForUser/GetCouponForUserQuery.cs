using Dogger.Domain.Models;
using MediatR;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetCouponForUser
{
    public class GetCouponForUserQuery : IRequest<PromotionCode?>
    {
        public User User { get; }

        public GetCouponForUserQuery(
            User user)
        {
            this.User = user;
        }
    }
}
