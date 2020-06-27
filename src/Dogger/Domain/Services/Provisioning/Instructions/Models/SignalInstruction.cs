namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public class SignalInstruction : IInstructionWithSignal
    {
        public string Type => "signal";

        public RetryPolicy RetryPolicy { get; }
        public string Signal { get; }

        public SignalInstruction(
            RetryPolicy retryPolicy, 
            string signal)
        {
            this.RetryPolicy = retryPolicy;
            this.Signal = signal;
        }
    }
}
