using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;



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

        public ContainerResponse(
            string id, 
            string image, 
            string[] names)
        {
            this.Id = id;
            this.Image = image;
            this.Names = names;
        }
    }
}
