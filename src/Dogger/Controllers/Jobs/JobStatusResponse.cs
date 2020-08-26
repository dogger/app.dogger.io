using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Jobs
{
    [ExcludeFromCodeCoverage]
    public class JobStatusResponse
    {
        public string StateDescription { get; set; } = null!;

        public bool IsEnded { get; set; }

        public bool IsSucceeded { get; set; }

        public bool IsFailed { get; set; }
    }
}
