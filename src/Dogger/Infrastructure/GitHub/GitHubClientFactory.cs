using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;
using Octokit;
using Serilog;

namespace Dogger.Infrastructure.GitHub
{
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly IGitHubClient gitHubClient;
        private readonly ILogger logger;
        private readonly IFlurlClient flurlClient;

        private readonly IOptionsMonitor<GitHubOptions> gitHubOptionsMonitor;

        public GitHubClientFactory(
            IGitHubClient gitHubClient,
            IFlurlClientFactory flurlClientFactory,
            ILogger logger,
            IOptionsMonitor<GitHubOptions> gitHubOptionsMonitor)
        {
            this.gitHubClient = gitHubClient;
            this.logger = logger;
            this.gitHubOptionsMonitor = gitHubOptionsMonitor;

            this.flurlClient = flurlClientFactory.Get("https://github.com/login/oauth/access_token");
        }

        public async Task<IGitHubClient> CreateInstallationClientAsync(long installationId)
        {
            if (installationId == default)
                throw new InvalidOperationException("No installation ID provided.");

            try
            {
                var installationToken = await this.gitHubClient.GitHubApps.CreateInstallationToken(installationId);
                return new GitHubClient(new ProductHeaderValue("pull-dog"))
                {
                    Credentials = new Credentials(
                        installationToken.Token)
                };
            }
            catch (NotFoundException ex)
            {
                this.logger.Error(ex, "The installation {InstallationId} was not found.", installationId);
                throw;
            }
        }

        public async Task<IGitHubClient> CreateInstallationInitiatorClientAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new InvalidOperationException("No code provided.");

            var options = this.gitHubOptionsMonitor.CurrentValue;
            if (options?.PullDog == null)
                throw new InvalidOperationException("Could not find Pull Dog settings.");

            var data = await this.flurlClient
                .Request()
                .SetQueryParam("client_id", options.PullDog.ClientId)
                .SetQueryParam("client_secret", options.PullDog.ClientSecret)
                .SetQueryParam("code", code)
                .PostAsync(new StringContent(string.Empty))
                .ReceiveString();

            var values = data
                .Split('&')
                .ToDictionary(
                    x => x.Split('=', 2)[0],
                    x => x.Split('=', 2)[1]);
            if (values.ContainsKey("error"))
                throw new InvalidCodeException(values["error"]);

            if (!values.ContainsKey("access_token"))
                throw new InvalidOperationException("An access token was not present in the response \"" + data + "\".");

            var accessToken = values["access_token"];
            return new GitHubClient(new ProductHeaderValue("pull-dog"))
            {
                Credentials = new Credentials(
                    accessToken)
            };
        }
    }

}
