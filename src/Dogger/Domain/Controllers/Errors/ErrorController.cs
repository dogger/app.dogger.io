using System;
using System.Diagnostics.CodeAnalysis;
using Dogger.Domain.Controllers.PullDog.Webhooks;
using Dogger.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Dogger.Domain.Controllers.Errors
{
    [ApiController]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorController : ControllerBase
    {
        private readonly IHostEnvironment hostEnvironment;

        public ErrorController(
            IHostEnvironment hostEnvironment)
        {
            this.hostEnvironment = hostEnvironment;
        }

        [Route("/errors/details")]
        [AllowAnonymous]
        public IActionResult ErrorDetails()
        {
            if (!ShouldDisplayErrorPage())
                return Problem();

            var context = this.HttpContext.Features.Get<IExceptionHandlerFeature>();
            var error = context.Error;
            return Problem(
                detail: error.StackTrace,
                title: $"{error.GetType().Name}: {error.Message}");
        }

        [Route("/errors/throw")]
        [AllowAnonymous]
        [ExcludeFromCodeCoverage]
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "This is a controller method.")]
        public IActionResult ThrowError()
        {
            throw new Exception("This is some error.");
        }

        private bool ShouldDisplayErrorPage()
        {
            return
                this.hostEnvironment.IsDevelopment() ||
                HasScopeHandler.HasScope(this.User, Scopes.ReadErrors) ||
                IsSignatureVerifiedWebhookRequest();
        }

        private bool IsSignatureVerifiedWebhookRequest()
        {
            return (bool?)this.HttpContext.Items[WebhooksController.WebhookSignatureVerificationKeyName] == true;
        }
    }
}
