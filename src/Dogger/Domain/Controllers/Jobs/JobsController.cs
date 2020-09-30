using System.Threading.Tasks;
using AutoMapper;
using Dogger.Domain.Services.Provisioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Controllers.Jobs
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly IMapper mapper;
        private readonly IProvisioningService provisioningService;

        public JobsController(
            IMapper mapper,
            IProvisioningService provisioningService)
        {
            this.mapper = mapper;
            this.provisioningService = provisioningService;
        }

        [HttpGet]
        [Route("{jobId}/status")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(JobStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Status(string jobId)
        {
            var job = await this.provisioningService.GetJobByIdAsync(jobId);
            if (job == null)
                return NotFound("Job not found.");

            var statusResult = job.Exception?.StatusResult;
            if (statusResult != null)
                return statusResult;

            return Ok(this.mapper.Map<JobStatusResponse>(job));
        }
    }
}
