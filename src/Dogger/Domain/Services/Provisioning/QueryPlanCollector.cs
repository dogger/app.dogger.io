using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dogger.Domain.Services.Provisioning
{
    public class QueryPlanCollector
    {
    }

    public class QueryPlan
    {
        public InstructionGroup[] Groups { get; }
    }

    public class InstructionGroup
    {
        public string Title { get; }
        public Instruction[] Instructions { get; }
    }

    public class Instruction
    {

    }
}
