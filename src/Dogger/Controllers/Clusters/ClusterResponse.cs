using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class ClusterResponse
    {
        public Guid Id { get; set; }

        public IEnumerable<InstanceResponse> Instances { get; set; } = null!;
    }
}
