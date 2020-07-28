using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Commands.PullDog.ChangePullDogPlan;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest;
using Dogger.Domain.Commands.PullDog.ProvisionPullDogEnvironment;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.PullDog.GetPullRequestDetailsFromCommitReference;
using Dogger.Domain.Queries.PullDog.GetRepositoriesForUser;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Infrastructure.Encryption;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.PullDog.Api
{
    [ApiController]
    [Route("api/pull-dog")]
    public class PullDogApiController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;
        private readonly IAesEncryptionHelper aesEncryptionHelper;

        public PullDogApiController(
            IMediator mediator,
            IMapper mapper,
            IAesEncryptionHelper aesEncryptionHelper)
        {
            this.mediator = mediator;
            this.mapper = mapper;
            this.aesEncryptionHelper = aesEncryptionHelper;
        }

        [HttpPost]
        [Route("change-plan")]
        public async Task<IActionResult> ChangePlan(ChangePlanRequest request)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            if (user.PullDogSettings == null)
                return BadRequest("Pull Dog has not been installed.");

            await this.mediator.Send(new ChangePullDogPlanCommand(
                user.Id, 
                request.PoolSize, 
                request.PlanId));

            return Ok();
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("provision")]
        public async Task<IActionResult> Provision(ProvisionRequest request)
        {
            if (request.PullRequestHandle == null && request.CommitReference == null)
                return BadRequest("Either a pull request handle or a branch reference must be specified.");

            var repository = await this.mediator.Send(new GetRepositoryByHandleQuery(request.RepositoryHandle));
            if (repository == null)
                return NotFound("Repository not found.");

            var decryptedKey = await aesEncryptionHelper.DecryptAsync(
                repository.PullDogSettings.EncryptedApiKey);
            if (decryptedKey != request.ApiKey)
                return Unauthorized("Bad API key.");

            var gitHubPullRequest = await this.mediator.Send(
                new GetPullRequestDetailsFromCommitReferenceQuery(
                    repository,
                    request.CommitReference!));
            if (githubPullRequest?.Number == null)
                return NotFound("Repository was found, but the pull request was not.");

            if (request.Configuration != null)
            {
                var pullDogPullRequest = await this.mediator.Send(
                    new EnsurePullDogPullRequestCommand(
                        repository,
                        request.PullRequestHandle!));

                await this.mediator.Send(
                    new OverrideConfigurationForPullRequestCommand(
                        pullDogPullRequest.Id,
                        request.Configuration));
            }

            var isReady = PullRequestReadinessHelper.IsReady(
                gitHubPullRequest.Draft,
                gitHubPullRequest.State.ToString(),
                gitHubPullRequest.User.Type?.ToString());
            if (!isReady)
                return NoContent();

            await this.mediator.Send(
                new ProvisionPullDogEnvironmentCommand(
                    gitHubPullRequest.Number.ToString(CultureInfo.InvariantCulture),
                    repository));

            return Ok("The environment is being provisioned.");
        }

        [HttpGet]
        [Route("settings")]
        [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettings()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            return Ok(new SettingsResponse()
            {
                PoolSize = user.PullDogSettings?.PoolSize,
                PlanId = user.PullDogSettings?.PlanId,
                ApiKey = user.PullDogSettings != null ?
                    await aesEncryptionHelper.DecryptAsync(user.PullDogSettings.EncryptedApiKey) :
                    null,
                IsInstalled = user.PullDogSettings != null
            });
        }

        [HttpGet]
        [Route("repositories")]
        [ProducesResponseType(typeof(RepositoriesResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRepositories()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            if (user.PullDogSettings == null)
                return NotFound("Pull Dog has not been installed yet.");

            var repositories = await this.mediator.Send(new GetRepositoriesForUserQuery(user.Id));
            return Ok(new RepositoriesResponse()
            {
                Repositories = repositories
                    .Select(mapper.Map<RepositoryResponse>)
                    .ToArray()
            });
        }
    }

}
