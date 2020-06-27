using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Domain.Services.Provisioning.Stages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Dogger.Domain.Services.Provisioning
{

    public class ProvisioningJob : IProvisioningJob
    {
        public string Id
        {
            get;
        }

        public bool IsEnded => IsSucceeded || IsFailed;

        public bool IsSucceeded
        {
            get; set;
        }

        public bool IsFailed => Exception != null;

        public StageUpdateException? Exception { get; set; }
        public IInstruction CurrentInstruction { get; set; }

        public ProvisioningJob(
            IInstruction currentInstruction)
        {
            this.CurrentInstruction = currentInstruction;

            Id = Guid.NewGuid().ToString();
        }
    }
}
