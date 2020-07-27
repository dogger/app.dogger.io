using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable 8618

namespace Dogger.Controllers.PullDog.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class WebhookPayload
    {
        public string? Action { get; set; }

        [JsonPropertyName("pull_request")]
        public PullRequestPayload? PullRequest { get;set; }

        public RepositoryPayload? Repository { get;set; }

        public UserPayload? Sender { get; set; }

        public IssuePayload? Issue { get; set; }

        public UserPayload? Pusher { get; set; }

        public CommentPayload? Comment { get; set; }

        public InstallationPayload Installation { get; set; }

        public CommitPayload[]? Commits { get; set; }

        [JsonPropertyName("repositories_added")]
        public InstallationRepositoryReferencePayload[]? RepositoriesAdded { get; set; }

        [JsonPropertyName("repositories_removed")]
        public InstallationRepositoryReferencePayload[]? RepositoriesRemoved { get; set; }

        [JsonPropertyName("ref")]
        public string? Reference { get; set; }
    }

    public class InstallationRepositoryReferencePayload
    {
        public long Id { get; set; }
    }

}
