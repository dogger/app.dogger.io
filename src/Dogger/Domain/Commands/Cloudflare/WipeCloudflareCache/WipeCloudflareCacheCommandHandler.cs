using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.AspNet.Options;
using Flurl.Http;
using Flurl.Http.Configuration;
using MediatR;
using Microsoft.Extensions.Options;

namespace Dogger.Domain.Commands.Cloudflare.WipeCloudflareCache
{
    [ExcludeFromCodeCoverage]
    public class WipeCloudflareCacheCommandHandler : IRequestHandler<WipeCloudflareCacheCommand>
    {
        private readonly IOptionsMonitor<CloudflareOptions> cloudflareCacheOptionsMonitor;
        private readonly IFlurlClient flurlClient;

        public WipeCloudflareCacheCommandHandler(
            IOptionsMonitor<CloudflareOptions> cloudflareCacheOptionsMonitor,
            IFlurlClientFactory flurlClientFactory)
        {
            this.cloudflareCacheOptionsMonitor = cloudflareCacheOptionsMonitor;
            this.flurlClient = flurlClientFactory.Get("https://api.cloudflare.com");
        }

        public async Task<Unit> Handle(WipeCloudflareCacheCommand request, CancellationToken cancellationToken)
        {
            var apiKey = this.cloudflareCacheOptionsMonitor.CurrentValue?.ApiKey;
            if (apiKey == null)
                throw new InvalidOperationException("Cloudflare API key missing.");

            await this.flurlClient
                .Request("/client/v4/zones/2b2cb5e45de57920b979258129cdfc0f/purge_cache")
                .WithHeader("X-Auth-Email", "mathias.lorenzen@live.com")
                .WithHeader("X-Auth-Key", apiKey)
                .PostJsonAsync(
                    new
                    {
                        purge_everything = true
                    },
                    cancellationToken);

            return Unit.Value;
        }
    }
}
