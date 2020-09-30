using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Dogger.Domain.Controllers.PullDog.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class CommentPayload
    {
        public long Id { get; set; }

        [JsonPropertyName("author_association")]
        public string? AuthorAssociation { get; set; }

        public string? Body { get; set; }

        public UserPayload? User { get; set; }
    }
}
