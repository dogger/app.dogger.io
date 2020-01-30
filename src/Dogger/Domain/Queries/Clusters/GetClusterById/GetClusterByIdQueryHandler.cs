using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Clusters.GetClusterById
{
    public class GetClusterByIdQueryHandler : IRequestHandler<GetClusterByIdQuery, Cluster?>
    {
        private readonly DataContext dataContext;

        public GetClusterByIdQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Cluster?> Handle(GetClusterByIdQuery request, CancellationToken cancellationToken)
        {
            var cluster = await this.dataContext
                .Clusters
                .Include(x => x.Instances)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return cluster;
        }
    }
}
