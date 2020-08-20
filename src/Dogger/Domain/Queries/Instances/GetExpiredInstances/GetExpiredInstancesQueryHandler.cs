using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Instances.GetExpiredInstances
{
    public class GetExpiredInstancesQueryHandler : IRequestHandler<GetExpiredInstancesQuery, Instance[]>
    {
        private readonly DataContext dataContext;

        public GetExpiredInstancesQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Instance[]> Handle(GetExpiredInstancesQuery request, CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            return await this.dataContext
                .Instances
                .AsQueryable()
                .Where(x => 
                    x.ExpiresAtUtc != null && 
                    x.ExpiresAtUtc < now)
                .ToArrayAsync(cancellationToken);
        }
    }
}
