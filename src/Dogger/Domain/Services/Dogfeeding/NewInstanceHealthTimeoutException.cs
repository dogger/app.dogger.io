using System;

namespace Dogger.Domain.Services.Dogfeeding
{
    public class NewInstanceHealthTimeoutException : Exception
    {
        public NewInstanceHealthTimeoutException(string message) : base(message)
        {
        }
    }
}
