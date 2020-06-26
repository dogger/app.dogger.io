using System;
using Dogger.Domain.Services.Provisioning.Instructions.Models;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IInstructionGroupCollector : IDisposable
    {
        IInstructionGroupCollector CollectGroup(string title);

        void CollectInstruction(IInstruction instruction);
    }
}
