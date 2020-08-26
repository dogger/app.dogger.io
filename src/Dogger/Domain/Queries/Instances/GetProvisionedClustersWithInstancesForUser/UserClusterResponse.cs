using System;
using System.Collections.Generic;



namespace Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser
{
    public class UserClusterResponse
    {
        public UserClusterResponse(
            Guid id, 
            IEnumerable<UserClusterInstanceResponse> instances)
        {
            this.Id = id;
            this.Instances = instances;
        }

        public Guid Id { get; }
        public IEnumerable<UserClusterInstanceResponse> Instances { get; }
    }
}
