using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.Clusters.EnsureClusterWithId
{
    public class EnsureClusterWithIdCommandHandler : IRequestHandler<EnsureClusterWithIdCommand, Cluster>
    {
        private readonly DataContext dataContext;

        public EnsureClusterWithIdCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Cluster> Handle(EnsureClusterWithIdCommand request, CancellationToken cancellationToken)
        {
            var existingCluster = await this.dataContext
                .Clusters
                .Include(x => x.User)
                .Include(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingCluster != null)
                return existingCluster;

            var newCluster = new Cluster
            {
                Id = request.Id
            };
            await this.dataContext.Clusters.AddAsync(newCluster, cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return newCluster;
        }
    }
}
