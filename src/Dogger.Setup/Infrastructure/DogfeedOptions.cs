using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure.AspNet.Options.Dogfeed
{
    [ExcludeFromCodeCoverage]
    public class DogfeedOptions
    {
        public string[]? Files { get; set; }

        public DockerHubOptions? DockerHub { get; set; }
    }

}
