using System;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public class StateUpdateException : Exception
    {
        public IActionResult? StatusResult { get; }

        public StateUpdateException(string message) : base(message)
        {
        }

        public StateUpdateException(string message, IActionResult statusResult) : this(message)
        {
            this.StatusResult = statusResult;
        }

        public StateUpdateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StateUpdateException(string message, Exception innerException, IActionResult statusResult) : this(message, innerException)
        {
            this.StatusResult = statusResult;
        }
    }
}
