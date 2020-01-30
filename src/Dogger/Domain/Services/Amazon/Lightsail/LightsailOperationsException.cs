using System;

namespace Dogger.Domain.Services.Amazon.Lightsail
{
    public class LightsailOperationsException : Exception
    {
        public LightsailOperationsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
