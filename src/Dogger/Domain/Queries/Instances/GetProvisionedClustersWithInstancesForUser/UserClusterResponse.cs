using System;
using System.Collections.Generic;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

namespace Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser
{
    public class UserClusterResponse
    {
        public Guid Id { get; set; }
        public IEnumerable<UserClusterInstanceResponse> Instances { get; set; }
    }
}
