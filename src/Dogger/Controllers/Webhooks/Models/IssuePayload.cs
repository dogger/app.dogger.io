using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Dogger.Controllers.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class IssuePayload
    {
        public long Id { get; set; }

        public string? State { get; set; }

        public bool Locked { get; set; }

        public int Number { get; set; }

        [JsonPropertyName("pull_request")]
        public IssuePullRequestPayload? PullRequest { get; set; }
    }

}
