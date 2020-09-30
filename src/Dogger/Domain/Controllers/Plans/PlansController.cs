using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Commands.Instances.ProvisionDemoInstance;
using Dogger.Domain.Commands.Instances.ProvisionInstanceForUser;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Payment.GetActivePaymentMethodForUser;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Controllers.Plans
{
    [ApiController]
    [Route("api/plans")]
    public class PlansController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IMediator mediator;

        public PlansController(
            IMapper mapper,
            IMediator mediator)
        {
            this.mapper = mapper;
            this.mediator = mediator;
        }

        [HttpGet]
        [Route("")]
        [AllowAnonymous]
        [ResponseCache(Duration = 1 * 60 * 60 * 24)]
        public async Task<IQueryable<PlanResponse>> Get()
        {
            var relevantPlans = await this.mediator.Send(new GetSupportedPlansQuery());
            return this.mapper.ProjectTo<PlanResponse>(relevantPlans.AsQueryable());
        }

        [HttpGet]
        [Route("demo")]
        [AllowAnonymous]
        [ResponseCache(Duration = 1 * 60 * 60 * 24)]
        public async Task<PlanResponse> GetDemo()
        {
            var demoPlan = await this.mediator.Send(new GetDemoPlanQuery());
            return this.mapper.Map<PlanResponse>(demoPlan);
        }

        [HttpPost]
        [Route("provision/demo")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PlanProvisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProvisionDemo()
        {
            try
            {
                var authenticatedUser = this.User?.Identity?.Name != null ? 
                    await this.mediator.Send(new EnsureUserForIdentityCommand(this.User)) :
                    null;

                var job = await this.mediator.Send(new ProvisionDemoInstanceCommand()
                {
                    AuthenticatedUserId = authenticatedUser?.Id
                });
                return Ok(this.mapper.Map<PlanProvisionResponse>(job));
            }
            catch (DemoInstanceAlreadyProvisionedException)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "ALREADY_PROVISIONED"
                });
            }
        }

        [HttpPost]
        [Route("provision/{planId}")]
        [ProducesResponseType(typeof(PlanProvisionResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> ProvisionPlan(string planId)
        {
            var user = await this.mediator.Send(new EnsureUserForIdentityCommand(this.User));
            var paymentMethod = await this.mediator.Send(new GetActivePaymentMethodForUserQuery(user));
            if (paymentMethod == null)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "NO_PAYMENT_METHOD"
                });
            }

            var plan = await this.mediator.Send(new GetPlanByIdQuery(planId));
            if (plan == null)
            {
                return ValidationProblem(new ValidationProblemDetails()
                {
                    Type = "PLAN_NOT_FOUND"
                });
            }

            var job = await this.mediator.Send(new ProvisionInstanceForUserCommand(
                plan,
                user));

            return Ok(this.mapper.Map<PlanProvisionResponse>(job));
        }
    }
}
