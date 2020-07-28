using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.PullDog.Webhooks.Models
{
    [ExcludeFromCodeCoverage]
    public class UserPayload
    {
        public string? Login { get; set; }

        public int Id { get; set; }

        public string? Type { get; set; }
    }
}
