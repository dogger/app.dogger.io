using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Infrastructure.Ssh
{
    public interface ISshClient : IDisposable
    {
        Task TransferFileAsync(
            RetryPolicy retryPolicy,
            string filePath,
            byte[] contents);

        Task<string> ExecuteCommandAsync(
            RetryPolicy retryPolicy, 
            string commandText,
            Dictionary<string, string?>? arguments = null);
    }
}
