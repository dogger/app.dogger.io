using System;

namespace Dogger.Infrastructure.Ssh
{
    public class CommandSanitizationException : Exception
    {
        public CommandSanitizationException(string message) : base(message)
        {
            
        }
    }
}
