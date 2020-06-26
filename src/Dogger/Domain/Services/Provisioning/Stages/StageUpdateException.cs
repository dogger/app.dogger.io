using System;
using Microsoft.AspNetCore.Mvc;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public class StageUpdateException : Exception
    {
        public IActionResult? StatusResult { get; }

        public StageUpdateException(string message) : base(message)
        {
        }

        public StageUpdateException(string message, IActionResult statusResult) : this(message)
        {
            this.StatusResult = statusResult;
        }

        public StageUpdateException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public StageUpdateException(string message, Exception innerException, IActionResult statusResult) : this(message, innerException)
        {
            this.StatusResult = statusResult;
        }
    }
}
