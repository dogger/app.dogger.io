namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public class InstructionGroup
    {
        public InstructionGroup(string title)
        {
            this.Title = title;
        }

        public string Title { get; }

        public IInstruction[]? Instructions { get; set; }
        public InstructionGroup[]? Groups { get; set; }
    }
}
