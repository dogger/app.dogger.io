using System;
using Amazon.Lightsail.Model;

namespace Dogger.Domain.Services.Amazon.Lightsail
{
    public class LightsailOperationException : Exception
    {
        public Operation Operation { get; }

        public LightsailOperationException(
            Operation operation)
        {
            this.Operation = operation;
        }
    }
}
