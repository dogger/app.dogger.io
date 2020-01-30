using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure.Ssh
{
    [ExcludeFromCodeCoverage]
    public class SshCommandExecutionException : Exception
    {
        public string CommandText { get; }
        public SshCommandResult Result { get; }

        public SshCommandExecutionException(
            string commandText, 
            SshCommandResult result)
        {
            this.CommandText = commandText;
            this.Result = result;
        }
    }
}
