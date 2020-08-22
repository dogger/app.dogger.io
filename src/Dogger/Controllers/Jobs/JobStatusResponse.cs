using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Jobs
{
    [ExcludeFromCodeCoverage]
    public class JobStatusResponse
    {
        public JobStatusResponse(
            string stateDescription, 
            bool isEnded, 
            bool isSucceeded, 
            bool isFailed)
        {
            StateDescription = stateDescription;
            IsEnded = isEnded;
            IsSucceeded = isSucceeded;
            IsFailed = isFailed;
        }

        public string StateDescription { get; set; }

        public bool IsEnded { get; set; }

        public bool IsSucceeded { get; set; }

        public bool IsFailed { get; set; }
    }
}
