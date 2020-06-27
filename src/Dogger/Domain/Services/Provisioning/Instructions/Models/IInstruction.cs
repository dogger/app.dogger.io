namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{

    public interface IInstruction
    {
        RetryPolicy RetryPolicy { get; }
        string Type { get; }

        InstructionGroup Group { get; }
    }
}
