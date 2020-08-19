using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Queries.Payment.GetCouponByCode;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.Deals
{
    [ApiController]
    [Route("api/deals")]
    public class DealsController : ControllerBase
    {
        private readonly IMediator mediator;

        public DealsController(
            IMediator mediator)
        {
            this.mediator = mediator;
        }

        [HttpPost]
        [Route("appsumo/apply")]
        [AllowAnonymous]
        public async Task<IActionResult> ApplyAppSumo([FromBody] ApplyAppSumoRequest request)
        {
            if (!request.Code.StartsWith("APPSUMO", StringComparison.InvariantCultureIgnoreCase))
                return BadRequest("Invalid coupon code for deal.");

            var appSumoPlan = await this.mediator.Send(new GetPullDogPlanFromSettingsQuery(
                "small_2_0",
                1));
            if (appSumoPlan == null)
                throw new InvalidOperationException("Could not find AppSumo plan.");

            var user = await this.mediator.Send(
                new InstallPullDogFromEmailsCommand(new []
                {
                    request.Email
                })
                {
                    Plan = appSumoPlan
                });

            var couponCode = await this.mediator.Send(new GetCouponByCodeQuery(request.Code));
            if (couponCode == null)
                return NotFound("Coupon code does not exist.");

            var applied = await this.mediator.Send(
                new ApplyCouponCodeForUserCommand(
                    user, 
                    request.Code));
            if (!applied)
                return BadRequest("The code was not applied.");

            return Ok();
        }
    }

}
