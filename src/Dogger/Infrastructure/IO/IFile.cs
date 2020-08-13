using System.Threading.Tasks;

namespace Dogger.Infrastructure.IO
{
    public interface IFile
    {
        Task<byte[]> ReadAllBytesAsync(string path);

        Task<string> ReadAllTextAsync(string path);
        string ReadAllText(string path);
    }
}
