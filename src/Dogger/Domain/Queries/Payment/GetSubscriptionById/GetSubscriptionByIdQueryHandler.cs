using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.Ioc;
using MediatR;
using Serilog;
using Stripe;

namespace Dogger.Domain.Queries.Payment.GetSubscriptionById
{
    public class GetSubscriptionByIdQueryHandler : IRequestHandler<GetSubscriptionByIdQuery, Subscription?>
    {
        private readonly ILogger logger;
        private readonly SubscriptionService? stripeSubscriptionService;

        public GetSubscriptionByIdQueryHandler(
            IOptionalService<SubscriptionService> stripeSubscriptionService,
            ILogger logger)
        {
            this.logger = logger;
            this.stripeSubscriptionService = stripeSubscriptionService.Value;
        }

        public async Task<Subscription?> Handle(
            GetSubscriptionByIdQuery request,
            CancellationToken cancellationToken)
        {
            if (this.stripeSubscriptionService == null)
                return null;

            try
            {
                var subscription = await this.stripeSubscriptionService.GetAsync(
                    request.Id,
                    default,
                    default,
                    cancellationToken);
                if (subscription == null)
                    return null;

                if (subscription.Status == "canceled")
                    return null;

                return subscription;
            }
            catch (StripeException ex) when (ex.StripeResponse.StatusCode == HttpStatusCode.NotFound)
            {
                this.logger.Warning(ex, "A subscription {SubscriptionId} was not found.", request.Id);
                return null;
            }
        }
    }
}

