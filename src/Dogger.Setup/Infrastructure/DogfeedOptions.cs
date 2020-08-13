using System.Collections.Generic;
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

        [NotLogged]
        public string[]? DockerComposeYmlFilePaths { get; set; }
        
        [NotLogged]
        public Dictionary<string, string>? BuildArguments { get; set; }

        public DockerRegistryOptions? DockerRegistry { get; set; }
    }

}
