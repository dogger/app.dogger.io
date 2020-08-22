using System.Threading.Tasks;
using Dogger.Domain.Commands.Amazon.ElasticContainerRegistry.EnsureRepositoryWithName;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.ElasticContainerRegistry.GetRepositoryLoginByRepositoryName;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.Registry
{
    [ApiController]
    [Route("api/registry")]
    public class RegistryController : ControllerBase
    {
        private readonly IMediator mediator;

        public RegistryController(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        [Route("demo/login")]
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLoginForDemo()
        {
            var authenticatedUser = User?.Identity?.Name != null ?
                await this.mediator.Send(new EnsureUserForIdentityCommand(User)) :
                null;

            return await HandleGetLoginAsync(authenticatedUser);
        }

        [Route("login")]
        [HttpGet]
        [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLogin()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(User));
            return await HandleGetLoginAsync(user);
        }

        private async Task<IActionResult> HandleGetLoginAsync(User? authenticatedUser)
        {
            var repositoryName = authenticatedUser?.Id.ToString() ?? "demo";

            var repositoryResponse = await this.mediator.Send(
                new EnsureRepositoryWithNameCommand(repositoryName)
                {
                    UserId = authenticatedUser?.Id
                });
            var login = await this.mediator.Send(
                new GetRepositoryLoginForUserQuery(repositoryResponse.WriteUser));
            return Ok(new LoginResponse(
                login.Username,
                login.Password,
                repositoryResponse.HostName));
        }
    }

}
