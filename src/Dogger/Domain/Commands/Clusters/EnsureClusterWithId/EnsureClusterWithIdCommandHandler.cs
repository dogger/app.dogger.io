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
            await this.dataContext
                .Clusters
                .Upsert(new Cluster
                {
                    Id = request.Id
                })
                .On(x => new
                {
                    x.Id
                })
                .RunAsync(cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            var cluster = await this.dataContext
                .Clusters
                .Include(x => x.User)
                .Include(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .Where(x => x.Id == request.Id)
                .SingleAsync(cancellationToken);
            return cluster;
        }
    }
}
