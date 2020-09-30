using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Controllers.PullDog.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class PullRequestPayload
    {
        public long Id { get; set; }

        public int Number { get; set; }

        public string State { get; set; } = null!;

        public HeadPayload? Head { get; set; } = null!;

        public bool Draft { get; set; }

        public UserPayload User { get; set; } = null!;
    }
}
