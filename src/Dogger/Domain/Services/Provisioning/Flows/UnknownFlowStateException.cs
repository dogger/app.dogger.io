using System;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class UnknownFlowStateException : Exception
    {
        public UnknownFlowStateException(string message) : base(message)
        {
            
        }
    }
}
