namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public class SshInstruction : IInstruction
    {
        public SshInstruction(
            RetryPolicy retryPolicy,
            string commandText)
        {
            this.RetryPolicy = retryPolicy;
            this.CommandText = commandText;
        }

        public RetryPolicy RetryPolicy { get; }

        public string Type => "ssh";

        public string CommandText { get; }
    }
}
