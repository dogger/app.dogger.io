using System;
using System.Collections.Generic;

namespace Dogger.Domain.Services.Provisioning.Stages
{
    public interface IProvisioningStage : IDisposable
    {
        void CollectInstructions(
            IInstructionGroupCollector instructionCollector);
    }

    public interface IInstructionGroupCollector : IDisposable
    {
        IInstructionGroupCollector CollectGroup(string title);

        void CollectInstruction(IInstruction instruction);
    }

    public class QueryPlan
    {
        public QueryPlan(InstructionGroup[] groups)
        {
            this.Groups = groups;
        }

        public InstructionGroup[] Groups { get; }
    }

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

    public interface IInstruction
    {
        string Type { get; }
    }

    public class SshInstruction : IInstruction
    {
        public SshInstruction(string commandText)
        {
            this.CommandText = commandText;
        }

        public string Type => "ssh";

        public string CommandText { get; }
    }

    public enum HttpInstructionVerb
    {
        Get,
        Put,
        Post,
        Patch,
        Delete
    }

    public class HttpInstruction : IInstruction
    {

        public HttpInstruction(
            HttpInstructionVerb verb, 
            string url)
        {
            this.Verb = verb;
            this.Url = url;
        }

        public string Type => "http";

        public HttpInstructionVerb Verb { get; set; }

        public IDictionary<string, string>? Headers { get; set; }

        public string Url { get; }

        public string? Body { get; set; }
    }
}
