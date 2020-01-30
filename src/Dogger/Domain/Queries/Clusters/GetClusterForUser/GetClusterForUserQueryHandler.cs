using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Clusters.GetClusterForUser
{
    public class GetClusterForUserQueryHandler : IRequestHandler<GetClusterForUserQuery, Cluster?>
    {
        private readonly DataContext dataContext;

        public GetClusterForUserQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Cluster?> Handle(GetClusterForUserQuery request, CancellationToken cancellationToken)
        {
            var clustersForUser = this.dataContext
                .Clusters
                .Include(x => x.Instances)
                .Where(x => x.UserId == request.UserId);

            if (request.ClusterId != null)
            {
                return await clustersForUser.FirstOrDefaultAsync(
                    x => x.Id == request.ClusterId.Value,
                    cancellationToken);
            }

            var clusters = await clustersForUser.ToListAsync(cancellationToken);
            if (clusters.Count > 1)
            {
                throw new ClusterQueryTooBroadException("More than one cluster was found for the given user, and no specific cluster ID was given.");
            }

            return clusters.SingleOrDefault();
        }
    }

}
