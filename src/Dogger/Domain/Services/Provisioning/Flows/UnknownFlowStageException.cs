using System;

namespace Dogger.Domain.Services.Provisioning.Flows
{
    public class UnknownFlowStageException : Exception
    {
        public UnknownFlowStageException(string message) : base(message)
        {
            
        }
    }
}
