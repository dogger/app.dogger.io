using System.Diagnostics.CodeAnalysis;
using Dogger.Controllers.Jobs;

namespace Dogger.Controllers.Plans
{
    [ExcludeFromCodeCoverage]
    public class PlanProvisionResponse
    {
        public JobStatusResponse? Status
        {
            get; set;
        }

        public string? JobId
        {
            get; set;
        }
    }
}
