using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Controllers.Webhooks.Handlers;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Helpers;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Infrastructure.AspNet.Options.GitHub;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Serilog;

namespace Dogger.Controllers.Webhooks
{
    [Route("api/webhooks")]
    [ApiController]
    public class WebhooksController : ControllerBase
    {
        private const string sha1Prefix = "sha1=";

        private readonly IMediator mediator;
        private readonly IConfigurationCommitPayloadHandler configurationCommitPayloadHandler;
        private readonly IEnumerable<IWebhookPayloadHandler> genericPayloadHandlers;

        private readonly IOptionsMonitor<GitHubOptions> gitHubOptionsMonitor;
        private readonly DataContext dataContext;

        //smee --url https://smee.io/EtHope2meSLYybsn --target http://localhost:14566/api/webhooks/github/pull-dog

        public WebhooksController(
            IMediator mediator,
            IConfigurationCommitPayloadHandler configurationCommitPayloadHandler,
            IEnumerable<IWebhookPayloadHandler> genericPayloadHandlers,
            IOptionsMonitor<GitHubOptions> gitHubOptionsMonitor,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.configurationCommitPayloadHandler = configurationCommitPayloadHandler;
            this.genericPayloadHandlers = genericPayloadHandlers;
            this.gitHubOptionsMonitor = gitHubOptionsMonitor;
            this.dataContext = dataContext;
        }

        [HttpPost]
        [Route("github/pull-dog")]
        [AllowAnonymous]
        public async Task<IActionResult> PullDogWebhook(
            WebhookPayload payload,
            CancellationToken cancellationToken)
        {
            if (!await IsGithubPushAllowedAsync())
                return NotFound();

            return await this.dataContext.ExecuteInTransactionAsync(
                async () =>
                {
                    if (this.configurationCommitPayloadHandler.CanHandle(payload))
                    {
                        await this.configurationCommitPayloadHandler.HandleAsync(payload);
                        return Ok();
                    }

                    var context = await GetWebhookPayloadContextAsync(payload);
                    if (context == null)
                        return NoContent();

                    var foundHandler = false;
                    foreach (var handler in this.genericPayloadHandlers)
                    {
                        if (!handler.CanHandle(payload))
                            continue;

                        await handler.HandleAsync(context);
                        foundHandler = true;
                    }

                    return foundHandler ? (IActionResult)Ok() : (IActionResult)NoContent();
                },
                default,
                cancellationToken);
        }

        private async Task<WebhookPayloadContext?> GetWebhookPayloadContextAsync(WebhookPayload payload)
        {
            if (payload.Repository == null)
                return null;

            var repositoryHandle = payload.Repository.Id.ToString(CultureInfo.InvariantCulture);
            var repository = await this.mediator.Send(new GetRepositoryByHandleQuery(repositoryHandle));
            if (repository == null)
                return null;

            if (repository.GitHubInstallationId != payload.Installation.Id)
                throw new InvalidOperationException($"Installation ID of {payload.Installation.Id} did not match the repository installation ID of {repository.GitHubInstallationId}.");

            var pullRequest = await GetPullRequestFromPayloadAsync(payload, repository);
            if (pullRequest == null)
                return null;

            return new WebhookPayloadContext(
                payload,
                repository.PullDogSettings,
                repository,
                pullRequest);
        }

        private async Task<PullDogPullRequest?> GetPullRequestFromPayloadAsync(
            WebhookPayload payload, 
            PullDogRepository repository)
        {
            var pullRequestHandle = GetPullRequestHandleFromPayload(payload);
            if (pullRequestHandle == null)
                return null;

            var pullRequest = await this.mediator.Send(
                new EnsurePullDogPullRequestCommand(
                    repository,
                    pullRequestHandle));
            return pullRequest;
        }

        private static string? GetPullRequestHandleFromPayload(WebhookPayload payload)
        {
            var pullRequestHandle = payload.PullRequest?.Number.ToString(CultureInfo.InvariantCulture);
            if (pullRequestHandle == null)
            {
                if (payload.Issue?.PullRequest == null)
                    return null;

                pullRequestHandle = payload.Issue.Number.ToString(CultureInfo.InvariantCulture);
            }

            return pullRequestHandle;
        }

        [SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms", Justification = "GitHub uses SHA1.")]
        private async Task<bool> IsGithubPushAllowedAsync()
        {
            Request.Headers.TryGetValue(
                "X-Hub-Signature",
                out StringValues signatureWithPrefix);

            using var reader = new StreamReader(Request.Body);
            reader.BaseStream.Seek(0, SeekOrigin.Begin);

            var payload = await reader.ReadToEndAsync();

            var signatureWithPrefixString = (string)signatureWithPrefix;
            if (!signatureWithPrefixString.StartsWith(sha1Prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var options = this.gitHubOptionsMonitor.CurrentValue.PullDog;
            if (options?.WebhookSecret == null)
                throw new InvalidOperationException("The webhook secret could not be found.");

            var signature = signatureWithPrefixString.Substring(sha1Prefix.Length);
            var secret = Encoding.UTF8.GetBytes(options.WebhookSecret);
            var payloadBytes = Encoding.UTF8.GetBytes(payload);

            using var sha = new HMACSHA1(secret);
            var hash = sha.ComputeHash(payloadBytes);

            var hashString = StringHelper.ToHexadecimal(hash);
            var isMatch = hashString.Equals(signature, StringComparison.InvariantCulture);
            return isMatch;
        }
    }

}
