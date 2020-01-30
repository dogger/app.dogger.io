using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class FileRequest
    {
        public string Path { get; set; }
        public string Contents { get; set; }
    }
}
