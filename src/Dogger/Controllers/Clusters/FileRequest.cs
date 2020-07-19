using System.Diagnostics.CodeAnalysis;
using Destructurama.Attributed;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class FileRequest
    {
        public string Path { get; set; } = null!;

        [NotLogged]
        public byte[] Contents { get; set; } = null!;
    }
}
