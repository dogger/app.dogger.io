using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser
{
    public class ApplyCouponCodeForUserCommand : IRequest<bool>
    {
        public User User { get; }
        public string CouponCode { get; }

        public ApplyCouponCodeForUserCommand(
            User user,
            string couponCode)
        {
            this.User = user;
            this.CouponCode = couponCode;
        }
    }
}

