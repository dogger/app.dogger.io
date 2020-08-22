using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Dogger.Controllers.Clusters
{
    [ExcludeFromCodeCoverage]
    public class ClusterResponse
    {
        public ClusterResponse(Guid id, IEnumerable<InstanceResponse> instances)
        {
            this.Id = id;
            this.Instances = instances;
        }

        public Guid Id { get; set; }

        public IEnumerable<InstanceResponse> Instances { get; set; }
    }
}
