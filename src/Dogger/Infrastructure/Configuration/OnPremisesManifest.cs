using Dogger.Infrastructure.AspNet.Options;
using Microsoft.Extensions.Configuration;

namespace Dogger.Infrastructure.Configuration
{
    /// <summary>
    /// This class describes what services are available to the on-prem solution, by looking at what has been configured. For instance, Stripe is unavailable if the optional secret has not been specified.
    /// </summary>
    public class OnPremisesManifest : IOnPremisesManifest
    {
        private readonly IConfiguration configuration;

        public bool HasStripe
        {
            get
            {
                var options = Get<StripeOptions>();
                return
                    !string.IsNullOrWhiteSpace(options?.SecretKey);
            }
        }

        public bool HasAuth0
        {
            get
            {
                var options = Get<Auth0Options>();
                return
                    !string.IsNullOrWhiteSpace(options?.ClientId) &&
                    !string.IsNullOrWhiteSpace(options?.ClientSecret);
            }
        }

        private T Get<T>()
        {
            return this.configuration.GetSection<T>();
        }

        public OnPremisesManifest(
            IConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}
