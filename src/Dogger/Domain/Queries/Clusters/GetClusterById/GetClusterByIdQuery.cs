using System;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.Clusters.GetClusterById
{
    public class GetClusterByIdQuery : IRequest<Cluster?>
    {
        public GetClusterByIdQuery(Guid id)
        {
            this.Id = id;
        }

        public Guid Id { get; }
    }
}
