using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Payment.AdjustUserBalance;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Queries.Payment.GetCouponByCode;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;

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
            var codeRequested = request.Code.ToUpperInvariant();
            if (!codeRequested.StartsWith("APPSUMO", StringComparison.InvariantCulture))
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

            var couponCode = await this.mediator.Send(new GetCouponByCodeQuery(codeRequested));
            if (couponCode == null)
                return NotFound("Coupon code does not exist.");

            var applied = await this.mediator.Send(new ApplyCouponCodeForUserCommand(user, codeRequested));
            if (!applied)
                return BadRequest("The code was not applied.");

            await this.mediator.Send(new AdjustUserBalanceCommand(
                user,
                GetAmountToCompensateForInHundreds(couponCode, appSumoPlan),
                $"DEAL_{codeRequested}"));

            return Ok();
        }

        private static int GetAmountToCompensateForInHundreds(PromotionCode couponCode, PullDogPlan appSumoPlan)
        {
            var percentageOff = couponCode.Coupon.PercentOff;
            if (percentageOff == null)
                throw new InvalidOperationException("Expected AppSumo coupon to have a percentage off.");

            var percentageOfOriginalPrice = 100 - percentageOff.Value;
            var monthlyPriceAfterPercentageOff = (int) Math.Floor((appSumoPlan.PriceInHundreds / 100m) * percentageOfOriginalPrice);

            var yearlyPriceAfterPercentageOff = monthlyPriceAfterPercentageOff * 12;
            return yearlyPriceAfterPercentageOff;
        }
    }

}
