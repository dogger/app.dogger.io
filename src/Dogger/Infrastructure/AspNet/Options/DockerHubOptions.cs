using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class DockerHubOptions
    {
        [LogMasked(ShowFirst = 2)]
        public string? Username { get; set; }

        [NotLogged]
        public string? Password { get; set; }
    }
}
