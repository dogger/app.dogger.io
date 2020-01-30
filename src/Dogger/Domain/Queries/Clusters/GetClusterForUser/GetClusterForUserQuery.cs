using System;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Clusters.GetClusterForUser
{
    public class GetClusterForUserQuery : IRequest<Cluster?>
    {
        public Guid UserId { get; }

        public Guid? ClusterId { get; set; }

        public GetClusterForUserQuery(
            Guid userId)
        {
            this.UserId = userId;
        }
    }
}
