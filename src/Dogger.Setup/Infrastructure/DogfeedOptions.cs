using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;
using Dogger.Infrastructure.AspNet.Options;

namespace Dogger.Setup.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class DogfeedOptions
    {
        [NotLogged]
        public string[]? AdditionalFilePaths { get; set; }

        public string[]? DockerComposeYmlFilePaths { get; set; }

        public DockerRegistryOptions? DockerRegistry { get; set; }
    }

}
