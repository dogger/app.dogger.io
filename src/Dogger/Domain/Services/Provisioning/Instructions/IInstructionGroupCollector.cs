using System;
using System.Collections.Generic;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IInstructionGroupCollector : IDisposable
    {
        IInstructionGroupCollector CollectGroup(string title);

        void CollectInstruction(IInstruction instruction);

        void CollectInstructionWithSignal(string signal);
        void CollectInstructionWithSignal(string signal, IInstruction instruction);
        void CollectInstructionWithSignal(IInstructionWithSignal instruction);

        void CollectFromStages(params Func<IProvisioningStageFactory, IProvisioningStage>[] stageFactories);

        IReadOnlyList<IInstruction> RetrieveCollectedInstructions();
    }
}
