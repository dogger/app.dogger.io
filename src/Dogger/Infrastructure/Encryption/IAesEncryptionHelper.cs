using System.Threading.Tasks;

namespace Dogger.Infrastructure.Encryption
{
    public interface IAesEncryptionHelper
    {
        Task<byte[]> EncryptAsync(string plainText);
        Task<string> DecryptAsync(byte[] cipherText);
    }
}
