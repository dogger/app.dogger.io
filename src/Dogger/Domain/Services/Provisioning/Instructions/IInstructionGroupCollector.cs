using System;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IInstructionGroupCollector : IDisposable
    {
        IInstructionGroupCollector CollectGroup(string title);

        void CollectInstruction(IInstruction instruction);

        void CollectInstructionWithSignal(string signal);
        void CollectInstructionWithSignal(string signal, IInstruction instruction);
        void CollectInstructionWithSignal(IInstructionWithSignal instruction);
    }
}
