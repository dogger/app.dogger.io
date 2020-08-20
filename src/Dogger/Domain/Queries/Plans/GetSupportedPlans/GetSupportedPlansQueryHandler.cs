using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using MediatR;

namespace Dogger.Domain.Queries.Plans.GetSupportedPlans
{
    public class GetSupportedPlansQueryHandler : IRequestHandler<GetSupportedPlansQuery, IReadOnlyCollection<Plan>>
    {
        private readonly IAmazonLightsail amazonLightsailClient;

        public GetSupportedPlansQueryHandler(
            IAmazonLightsail amazonLightsailClient)
        {
            this.amazonLightsailClient = amazonLightsailClient;
        }

        public async Task<IReadOnlyCollection<Plan>> Handle(GetSupportedPlansQuery request, CancellationToken cancellationToken)
        {
            var plans = await amazonLightsailClient.GetBundlesAsync(new GetBundlesRequest()
            {
                IncludeInactive = true
            }, cancellationToken);

            return plans.Bundles
                .Where(x => x.IsActive)
                .Where(x => x.SupportedPlatforms.Contains("LINUX_UNIX"))
                .Select(MapAmazonBundleToPlanResponse)
                .ToArray();
        }

        private static Plan MapAmazonBundleToPlanResponse(Bundle bundle)
        {
            return new Plan(
                bundle.BundleId,
                GetDoggerPriceFromBundle(bundle),
                bundle,
                GetPullDogPlansFromRamSize(bundle));
        }

        private static PullDogPlan[] GetPullDogPlansFromRamSize(Bundle bundle)
        {
            var ramSizeInMegabytes = (int)(bundle.RamSizeInGb * 1024);
            return new[]
            {
                new PullDogPlan(
                    $"personal_{ramSizeInMegabytes}",
                    GetPullDogPriceFromBundle(bundle, 1),
                    1),
                new PullDogPlan(
                    $"pro_{ramSizeInMegabytes}",
                    GetPullDogPriceFromBundle(bundle, 2),
                    2),
                new PullDogPlan(
                    $"business_{ramSizeInMegabytes}",
                    GetPullDogPriceFromBundle(bundle, 5),
                    5)
            };
        }

        private static int Normalize(double priceInHundreds)
        {
            return (int)Math.Ceiling(priceInHundreds / 100d) * 100;
        }

        private static int GetPullDogPriceFromBundle(Bundle bundle, int amount)
        {
            var price = GetBasePriceFromBundle(bundle) * amount * 4;
            return Normalize(price);
        }

        private static int GetBasePriceFromBundle(Bundle bundle)
        {
            return GetPriceInHundredsFromBundle(bundle);
        }

        private static int GetDoggerPriceFromBundle(Bundle bundle)
        {
            return Normalize(
                GetBasePriceFromBundle(bundle) + 
                GetDoggerEarningsFromBundle(bundle));
        }

        private static int GetPriceInHundredsFromBundle(Bundle bundle)
        {
            return (int)(bundle.Price * 100);
        }

        private static int GetDoggerEarningsFromBundle(Bundle bundle)
        {
            var priceInHundreds = GetPriceInHundredsFromBundle(bundle);

            var earnings = 1_00;
            earnings += priceInHundreds / 4;

            return earnings;
        }
    }
}
