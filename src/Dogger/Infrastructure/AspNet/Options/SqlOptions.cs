using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class SqlOptions
    {
        [NotLogged]
        public string? ConnectionString { get; set; }
    }
}
