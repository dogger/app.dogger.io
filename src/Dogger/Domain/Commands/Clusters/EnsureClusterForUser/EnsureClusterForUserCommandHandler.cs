using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Models.Builders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.Clusters.EnsureClusterForUser
{
    public class EnsureClusterForUserCommandHandler : IRequestHandler<EnsureClusterForUserCommand, Cluster>
    {
        private readonly DataContext dataContext;

        public EnsureClusterForUserCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Cluster> Handle(EnsureClusterForUserCommand request, CancellationToken cancellationToken)
        {
            var cluster = await this.dataContext
                .Clusters
                .Include(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .SingleOrDefaultAsync(
                    x => x.Name == request.ClusterName &&
                        x.UserId == request.UserId,
                    cancellationToken);
            if (cluster != null)
                return cluster;

            var newCluster = new ClusterBuilder()
                .WithUser(request.UserId)
                .WithName(request.ClusterName)
                .Build();
            await this.dataContext.Clusters.AddAsync(newCluster, cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return newCluster;
        }
    }
}
