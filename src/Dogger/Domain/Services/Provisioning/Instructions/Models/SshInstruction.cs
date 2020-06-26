namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public class SshInstruction : IInstruction
    {
        public SshInstruction(string commandText)
        {
            this.CommandText = commandText;
        }

        public string Type => "ssh";

        public string CommandText { get; }
    }
}
