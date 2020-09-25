using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Dogger.Domain.Commands.Clusters.EnsureClusterWithId
{
    public class EnsureClusterWithIdCommandHandler : IRequestHandler<EnsureClusterWithIdCommand, Cluster>
    {
        private readonly DataContext dataContext;
        private readonly ILogger logger;

        public EnsureClusterWithIdCommandHandler(
            DataContext dataContext,
            ILogger logger)
        {
            this.dataContext = dataContext;
            this.logger = logger;
        }

        public async Task<Cluster> Handle(EnsureClusterWithIdCommand request, CancellationToken cancellationToken)
        {
            var existingCluster = await GetExistingClusterAsync(request, cancellationToken);
            if (existingCluster != null)
                return existingCluster;
            
            try
            {
                var newCluster = new Cluster
                {
                    Id = request.Id
                };
                await this.dataContext.Clusters.AddAsync(newCluster, cancellationToken);
                await this.dataContext.SaveChangesAsync(cancellationToken);

                return newCluster;
            }
            catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
            {
                var conflictingCluster = await GetExistingClusterAsync(request, cancellationToken);
                return conflictingCluster!;
            }
            catch (DbUpdateException dbe) when (dbe.InnerException is SqlException sqe)
            {
                this.logger.Error("An unknown database error occured while ensuring a cluster with code {SqlCodeNumber}.", sqe.Number);
                throw;
            }
            catch (DbUpdateException dbx)
            {
                this.logger.Error(dbx, "An unknown database error occured.");
                throw;
            }
        }

        private async Task<Cluster?> GetExistingClusterAsync(EnsureClusterWithIdCommand request, CancellationToken cancellationToken)
        {
            return await this.dataContext
                .Clusters
                .Include(x => x.User)
                .Include(x => x.Instances)
                .ThenInclude(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .Where(x => x.Id == request.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
