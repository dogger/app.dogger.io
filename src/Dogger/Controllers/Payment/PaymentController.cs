using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Commands.Payment;
using Dogger.Domain.Commands.Payment.SetActivePaymentMethodForUser;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Payment;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Controllers.Payment
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
            var user = await mediator.Send(new EnsureUserForIdentityCommand(User));
            var paymentMethod = await mediator.Send(new GetActivePaymentMethodForUserQuery(user));
            return Ok(mapper.Map<PaymentMethodResponse>(paymentMethod));
        }

        [Route("methods/{paymentMethodId}")]
        [HttpPut]
        public async Task<IActionResult> AddPaymentMethod(string paymentMethodId)
        {
            var user = await mediator.Send(new EnsureUserForIdentityCommand(User));
            await mediator.Send(
                new SetActivePaymentMethodForUserCommand(
                    user,
                    paymentMethodId));

            return Ok();
        }
    }
}
