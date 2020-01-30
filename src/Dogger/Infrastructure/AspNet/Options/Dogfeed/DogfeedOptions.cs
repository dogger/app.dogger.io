using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure.AspNet.Options.Dogfeed
{
    [ExcludeFromCodeCoverage]
    public class DogfeedOptions
    {
        public string? DockerComposeYmlContents { get; set; }

        public DockerHubOptions? DockerHub { get; set; }
        public DogfeedElasticsearchOptions? Elasticsearch { get; set; }
    }

}
