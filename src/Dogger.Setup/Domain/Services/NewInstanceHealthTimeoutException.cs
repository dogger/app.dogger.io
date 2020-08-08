using System;

namespace Dogger.Setup.Domain.Services
{
    public class NewInstanceHealthTimeoutException : Exception
    {
        public NewInstanceHealthTimeoutException(string message) : base(message)
        {
        }
    }
}
