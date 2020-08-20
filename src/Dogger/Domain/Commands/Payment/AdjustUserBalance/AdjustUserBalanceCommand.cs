using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.Payment.AdjustUserBalance
{
    public class AdjustUserBalanceCommand : IRequest<Unit>
    {
        public User User { get; }

        public int AdjustmentInHundreds { get; }

        public string IdempotencyId { get; }

        public AdjustUserBalanceCommand(
            User user,
            int adjustmentInHundreds,
            string idempotencyId)
        {
            this.User = user;
            this.AdjustmentInHundreds = adjustmentInHundreds;
            this.IdempotencyId = idempotencyId;
        }
    }
}
