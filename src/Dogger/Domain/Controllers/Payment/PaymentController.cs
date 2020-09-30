using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Commands.Payment.ApplyCouponCodeForUser;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Payment.GetActivePaymentMethodForUser;
using Dogger.Domain.Queries.Payment.GetCouponForUser;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Controllers.Payment
{
    [Route("api/payment")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMediator mediator;
        private readonly IMapper mapper;

        public PaymentController(
            IMediator mediator,
            IMapper mapper)
        {
            this.mediator = mediator;
            this.mapper = mapper;
        }

        [Route("methods/current")]
        [HttpGet]
        [ProducesResponseType(typeof(PaymentMethodResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentPaymentMethod()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(this.User));
            var paymentMethod = await this.mediator.Send(new GetActivePaymentMethodForUserQuery(user));
            return Ok(this.mapper.Map<PaymentMethodResponse>(paymentMethod));
        }

        [Route("methods/{paymentMethodId}")]
        [HttpPut]
        public async Task<IActionResult> AddPaymentMethod(string paymentMethodId)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(this.User));
            await this.mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    paymentMethodId));

            return Ok();
        }

        [Route("coupon")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(CouponCodeResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCoupon()
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(this.User));
            var coupon = await this.mediator.Send(new GetCouponForUserQuery(user));
            if (coupon == null)
                return NoContent();

            return Ok(this.mapper.Map<CouponCodeResponse>(coupon));
        }

        [Route("coupon/{code}")]
        [HttpPost]
        [ProducesResponseType(typeof(ApplyCouponResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ApplyCoupon(string code)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(this.User));

            var wasApplied = await this.mediator.Send(new ApplyCouponCodeForUserCommand(user, code));
            return Ok(new ApplyCouponResponse(wasApplied));
        }
    }
}
