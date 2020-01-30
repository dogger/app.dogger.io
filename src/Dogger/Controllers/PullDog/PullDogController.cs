using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.PullDog
{
    [Controller]
    [Route("pull-dog")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PullDogController : Controller
    {
        private readonly IMediator mediator;

        public PullDogController(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("install/github/instructions")]
        public IActionResult InstallGitHubInstructions()
        {
            return View("~/Controllers/PullDog/Views/InstallGitHub.cshtml");
        }

        [HttpGet]
        [AllowAnonymous]
        [Route("install/github")]
        public async Task<IActionResult> InstallGitHub(
            [FromQuery(Name = "code")] string code,
            [FromQuery(Name = "installation_id")] long installationId,
            [FromQuery(Name = "setup_action")] string setupAction)
        {
            if (setupAction != "install")
                return Redirect("/dashboard/pull-dog");

            await this.mediator.Send(new InstallPullDogFromGitHubCommand(
                code,
                installationId));

            return RedirectToAction("InstallGitHubInstructions");
        }
    }

}
