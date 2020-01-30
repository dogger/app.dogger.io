using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Dogger.Controllers.Webhooks
{
    [ExcludeFromCodeCoverage]
    public class HeadPayload
    {
        [JsonPropertyName("ref")]
        public string? Reference { get; set; }

        public string? Sha { get; set; }

        public UserPayload? User { get; set; }
    }
}
