using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Domain.Controllers.Repl
{
    [ExcludeFromCodeCoverage]
    public class CommandRequest
    {
        [Required]
        public string? Command { get; set; }
    }
}
