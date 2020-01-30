using System;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Infrastructure.GitHub
{
    [ExcludeFromCodeCoverage]
    public class InvalidCodeException : Exception
    {
        public InvalidCodeException(string message) : base(message)
        {
        }

        public InvalidCodeException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
