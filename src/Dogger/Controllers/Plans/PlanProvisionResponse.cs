using System.Diagnostics.CodeAnalysis;
using Dogger.Controllers.Jobs;

namespace Dogger.Controllers.Plans
{
    [ExcludeFromCodeCoverage]
    public class PlanProvisionResponse
    {
        public PlanProvisionResponse(JobStatusResponse status, string jobId)
        {
            this.Status = status;
            this.JobId = jobId;
        }

        public JobStatusResponse Status { get; }

        public string JobId { get; }
    }
}
