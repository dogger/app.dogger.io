using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Dogger.Infrastructure.AspNet.Options;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;

namespace Dogger.Infrastructure.Auth.Auth0
{
    [ExcludeFromCodeCoverage]
    public class ManagementApiClientFactory : IManagementApiClientFactory
    {
        private readonly IOptionsMonitor<Auth0Options> auth0OptionsMonitor;
        private readonly IFlurlClient flurlClient;

        public ManagementApiClientFactory(
            IFlurlClientFactory flurlClientFactory,
            IOptionsMonitor<Auth0Options> auth0OptionsMonitor)
        {
            this.flurlClient = flurlClientFactory.Get("https://dogger.eu.auth0.com/oauth/token");
            this.auth0OptionsMonitor = auth0OptionsMonitor;
        }

        public async Task<IManagementApiClient> CreateAsync()
        {
            var options = this.auth0OptionsMonitor.CurrentValue;

            var response = await this.flurlClient
                .Request()
                .PostUrlEncodedAsync(new
                {
                    grant_type = "client_credentials",
                    client_id = options.ClientId,
                    client_secret = options.ClientSecret,
                    audience = "https://dogger.eu.auth0.com/api/v2/"
                })
                .ReceiveJson();
            var accessToken = (string)response.access_token;

            var domainUrl = new Uri(AuthConstants.Auth0Domain);
            return new ManagementApiClientProxy(
                new ManagementApiClient(
                    accessToken, 
                    domainUrl.Host));
        }
    }
}
