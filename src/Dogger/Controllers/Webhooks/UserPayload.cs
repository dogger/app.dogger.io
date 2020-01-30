using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Webhooks
{
    [ExcludeFromCodeCoverage]
    public class UserPayload
    {
        public string? Login { get; set; }

        public long Id { get; set; }
    }
}
