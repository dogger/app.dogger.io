namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public interface IInstructionWithSignal : IInstruction
    {
        string Signal { get; }
    }
}
