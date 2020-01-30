using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options.Dogfeed
{
    [ExcludeFromCodeCoverage]
    public class DogfeedElasticsearchOptions
    {
        [NotLogged]
        public string? InstancePassword { get; set; }

        [NotLogged]
        public string? ConfigurationYmlContents { get; set; }

        [NotLogged]
        public string? AdminKeyPassword { get; set; }

        [NotLogged]
        public string? AdminKeyContents { get; set; }

        [NotLogged]
        public string? AdminPemContents { get; set; }

        [NotLogged]
        public string? NodeKeyContents { get; set; }

        [NotLogged]
        public string? NodePemContents { get; set; }

        [NotLogged]
        public string? RootCaKeyContents { get; set; }

        [NotLogged]
        public string? RootCaPemContents { get; set; }
    }
}
