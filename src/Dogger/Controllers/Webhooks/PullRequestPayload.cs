using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Webhooks
{
    [ExcludeFromCodeCoverage]
    public class PullRequestPayload
    {
        public long Id { get; set; }

        public int Number { get; set; }

        public string? State { get; set; }

        public HeadPayload? Head { get; set; }

        public bool Draft { get; set; }
    }
}
