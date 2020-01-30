using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class CloudflareOptions
    {
        [NotLogged]
        public string? ApiKey { get; set; }
    }
}
