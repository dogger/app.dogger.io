using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class SlackOptions
    {
        [NotLogged]
        public string? IncomingUrl { get; set; }
    }
}
