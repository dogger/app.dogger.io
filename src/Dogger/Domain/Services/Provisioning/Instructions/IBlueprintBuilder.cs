using System;
using System.Collections.Generic;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning.Instructions.Models;
using Dogger.Domain.Services.Provisioning.Stages;

namespace Dogger.Domain.Services.Provisioning.Instructions
{
    public interface IBlueprintBuilder : IDisposable
    {
        IBlueprintBuilder AddGroup(string title);

        void AddInstruction(IInstruction instruction);

        void AddInstructionWithSignal(string signal);
        void AddInstructionWithSignal(string signal, IInstruction instruction);
        void AddInstructionWithSignal(IInstructionWithSignal instruction);

        Blueprint Build();
    }
}
