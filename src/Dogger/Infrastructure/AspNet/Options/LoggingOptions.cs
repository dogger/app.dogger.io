using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class LoggingOptions
    {
        [NotLogged]
        public string? ElasticsearchLoggingUrl { get; set; }
    }
}
