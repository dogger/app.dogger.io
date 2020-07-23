using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class UserPayload
    {
        public string? Login { get; set; }

        public long Id { get; set; }

        public string? Type { get; set; }
    }
}
