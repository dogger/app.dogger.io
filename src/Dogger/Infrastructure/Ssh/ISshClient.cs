using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Ssh
{
    public interface ISshClient : IDisposable
    {
        Task TransferFileAsync(
            SshRetryPolicy retryPolicy,
            string filePath,
            byte[] contents);

        Task<string> ExecuteCommandAsync(
            SshRetryPolicy retryPolicy,
            SshResponseSensitivity dataSensitivity,
            string commandText,
            Dictionary<string, string?>? arguments = null);
    }
}
