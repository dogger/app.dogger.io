using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Infrastructure.AspNet.Options
{
    [ExcludeFromCodeCoverage]
    public class EncryptionOptions
    {
        [NotLogged]
        public string? Pepper { get; set; }
    }
}
