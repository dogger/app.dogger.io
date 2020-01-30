using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dogger.Infrastructure.Ssh
{
    public interface ISshClient : IDisposable
    {

        Task<string> ExecuteCommandAsync(
            SshRetryPolicy retryPolicy, 
            string commandText,
            Dictionary<string, string?>? arguments = null);
    }
}
