using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class Auth0Options
    {
        [NotLogged]
        public string? ClientId { get; set; }

        [NotLogged]
        public string? ClientSecret { get; set; }
    }
}
