using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Stripe;

namespace Dogger.Domain.Commands.Payment.AdjustUserBalance
{
    public class AdjustUserBalanceCommandHandler : IRequestHandler<AdjustUserBalanceCommand, Unit>
    {
        public const string IdempotencyKeyName = "IdempotencyKey";

        private readonly CustomerBalanceTransactionService? stripeBalanceService;

        public AdjustUserBalanceCommandHandler(
            IOptionalService<CustomerBalanceTransactionService> stripeBalanceService)
        {
            this.stripeBalanceService = stripeBalanceService.Value;
        }

        public async Task<Unit> Handle(
            AdjustUserBalanceCommand request,
            CancellationToken cancellationToken)
        {
            if (this.stripeBalanceService == null)
                return Unit.Value;

            var idempotencyId = request.IdempotencyId.ToUpperInvariant();

            var alreadyMadeAdjustment = await HasBalanceAdjustmentAlreadyBeenMadeAsync(request, cancellationToken);
            if (alreadyMadeAdjustment)
                return Unit.Value;

            await this.stripeBalanceService.CreateAsync(
                request.User.StripeCustomerId,
                new CustomerBalanceTransactionCreateOptions()
                {
                    Amount = -request.AdjustmentInHundreds,
                    Description = "Idempotency ID: " + idempotencyId,
                    Metadata = new Dictionary<string, string>()
                    {
                        {
                            IdempotencyKeyName, idempotencyId
                        }
                    }
                },
                default,
                cancellationToken);

            return Unit.Value;
        }

        private async Task<bool> HasBalanceAdjustmentAlreadyBeenMadeAsync(
            AdjustUserBalanceCommand request, 
            CancellationToken cancellationToken)
        {
            if (this.stripeBalanceService == null)
                throw new InvalidOperationException("Stripe balance service was not instantiated.");

            return await this.stripeBalanceService
                .ListAutoPagingAsync(
                    request.User.StripeCustomerId,
                    default,
                    default,
                    cancellationToken)
                .Select(x => x.Metadata)
                .Where(x => x.ContainsKey(IdempotencyKeyName))
                .AnyAsync(
                    x => x[IdempotencyKeyName] == request.IdempotencyId,
                    cancellationToken);
        }
    }
}

