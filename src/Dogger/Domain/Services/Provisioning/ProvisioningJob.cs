using System;
using System.Linq;
using Destructurama.Attributed;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning
{

    public class ProvisioningJob : IProvisioningJob
    {
        private readonly Blueprint blueprint;

        private IInstruction? currentInstruction;

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

        [NotLogged]
        public ScheduleJobOptions? Options { get; }
        public IInstruction? CurrentInstruction => this.currentInstruction;

        public IInstruction IterateToNextInstruction()
        {
            if (this.currentInstruction == null)
            {
                var instruction = GetFirstInstructionInGroup(this.blueprint.InstructionGroups[0]);
                if(instruction == null)
                    throw new InvalidOperationException("Invalid blueprint with no instruction groups.");

                return this.currentInstruction = instruction;
            }

            throw new NotImplementedException();
        }

        private static IInstruction? GetFirstInstructionInGroup(InstructionGroup group)
        {
            if (group.Instructions != null)
                return group.Instructions[0];

            return group
                .Groups?
                .Select(GetFirstInstructionInGroup)
                .FirstOrDefault(x => x != null);
        }

        public ProvisioningJob(
            Blueprint blueprint,
            ScheduleJobOptions? options)
        {
            this.Options = options;
            this.blueprint = blueprint;

            Id = Guid.NewGuid().ToString();
        }
    }
}
