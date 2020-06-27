using System;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Ssh
{
    public interface ISshClientDecorator : IDisposable
    {
        Task<SshCommandResult> ExecuteCommandAsync(string text);
        Task ConnectAsync();
        Task TransferFileAsync(string filePath, byte[] contents);
    }
}
