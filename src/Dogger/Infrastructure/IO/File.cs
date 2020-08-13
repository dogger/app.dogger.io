using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.IO
{
    [ExcludeFromCodeCoverage]
    public class File : IFile
    {
        public async Task<byte[]> ReadAllBytesAsync(string path)
        {
            return await System.IO.File.ReadAllBytesAsync(path);
        }

        public async Task<string> ReadAllTextAsync(string path)
        {
            return await System.IO.File.ReadAllTextAsync(path);
        }

        public string ReadAllText(string path)
        {
            return System.IO.File.ReadAllText(path);
        }
    }
}
