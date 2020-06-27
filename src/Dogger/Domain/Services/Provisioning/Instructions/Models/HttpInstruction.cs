using System.Collections.Generic;

namespace Dogger.Domain.Services.Provisioning.Instructions.Models
{
    public class HttpInstruction : IInstruction
    {
        private RetryPolicy? retryPolicy;

        public HttpInstruction(
            HttpInstructionVerb verb, 
            string url)
        {
            this.Verb = verb;
            this.Url = url;
        }

        public RetryPolicy RetryPolicy
        {
            get =>
                this.retryPolicy ??
                (Verb == HttpInstructionVerb.Get ? 
                    RetryPolicy.AllowRetries : 
                    RetryPolicy.ProhibitRetries);
            set => this.retryPolicy = value;
        }

        public string Type => "http";

        public HttpInstructionVerb Verb { get; set; }

        public IDictionary<string, string>? Headers { get; set; }

        public string Url { get; }

        public string? Body { get; set; }
    }
}
