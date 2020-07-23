using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Dogger.Controllers.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class RepositoryPayload
    {
        public long Id { get; set; }

        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("private")]
        public bool IsPrivate { get; set; }

        [JsonPropertyName("default_branch")]
        public string? DefaultBranch { get; set; }

        public UserPayload? Owner { get; set; }
    }
}
