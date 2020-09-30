using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Controllers.Payment
{
    [ExcludeFromCodeCoverage]
    public class PaymentMethodResponse
    {
        public string? Id { get; set; }
        public string? Brand { get; set; }
    }
}
