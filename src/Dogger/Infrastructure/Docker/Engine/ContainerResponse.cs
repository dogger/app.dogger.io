using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Infrastructure.Docker.Engine
{
    [ExcludeFromCodeCoverage]
    public class ContainerResponse
    {
        [JsonPropertyName(nameof(Names))]
        public string[] Names { get; set; }
        
        [JsonPropertyName(nameof(Id))]
        public string Id { get; set; }
        
        [JsonPropertyName(nameof(Image))]
        public string Image { get; set; }
    }
}
