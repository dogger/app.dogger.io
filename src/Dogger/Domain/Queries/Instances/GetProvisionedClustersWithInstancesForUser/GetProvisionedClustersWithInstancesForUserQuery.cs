using System;
using System.Collections.Generic;
using MediatR;

namespace Dogger.Domain.Queries.Instances.GetProvisionedClustersWithInstancesForUser
{
    public class GetProvisionedClustersWithInstancesForUserQuery : IRequest<IReadOnlyList<UserClusterResponse>>
    {
        public Guid UserId { get; }

        public GetProvisionedClustersWithInstancesForUserQuery(
            Guid userId)
        {
            this.UserId = userId;
        }
    }
}
