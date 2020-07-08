using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
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
            await this.dataContext
                .Clusters
                .Upsert(new Cluster()
                {
                    UserId = request.UserId,
                    Name = request.ClusterName
                })
                .On(x => new
                {
                    x.Id,
                    x.Name
                })
                .RunAsync(cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            var cluster = await this.dataContext
                .Clusters
                .Include(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .SingleAsync(
                    x => x.Name == request.ClusterName &&
                        x.UserId == request.UserId,
                    cancellationToken);
            return cluster;
        }
    }
}
