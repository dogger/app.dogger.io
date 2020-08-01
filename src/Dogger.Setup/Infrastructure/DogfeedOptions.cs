using System.Diagnostics.CodeAnalysis;
using Dogger.Infrastructure.AspNet.Options;

namespace Dogger.Setup.Infrastructure
{
    [ExcludeFromCodeCoverage]
    public class DogfeedOptions
    {
        public string[]? Files { get; set; }

        public DockerHubOptions? DockerHub { get; set; }
    }

}
