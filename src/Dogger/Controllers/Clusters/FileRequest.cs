using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class FileRequest
    {
        public string Path { get; set; } = null!;
        public byte[] Contents { get; set; } = null!;
    }
}
