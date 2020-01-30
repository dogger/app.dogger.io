using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.Instances.GetInstanceByName
{
    public class GetInstanceByNameQueryHandler : IRequestHandler<GetInstanceByNameQuery, Instance?>
    {
        private readonly DataContext dataContext;

        public GetInstanceByNameQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Instance?> Handle(GetInstanceByNameQuery request, CancellationToken cancellationToken)
        {
            return await dataContext.Instances
                .Include(x => x.Cluster)
                .Include(x => x.PullDogPullRequest)
                .ThenInclude(x => x!.PullDogRepository)
                .ThenInclude(x => x!.PullDogSettings)
                .ThenInclude(x => x!.User)
                .FirstOrDefaultAsync(
                    x => x.Name == request.Name, 
                    cancellationToken: cancellationToken);
        }
    }
}
