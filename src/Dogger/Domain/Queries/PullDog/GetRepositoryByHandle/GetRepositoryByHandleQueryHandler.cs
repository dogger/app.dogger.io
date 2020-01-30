using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.PullDog.GetRepositoryByHandle
{
    public class GetRepositoryByHandleQueryHandler : IRequestHandler<GetRepositoryByHandleQuery, PullDogRepository?>
    {
        private readonly DataContext dataContext;

        public GetRepositoryByHandleQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<PullDogRepository?> Handle(GetRepositoryByHandleQuery request, CancellationToken cancellationToken)
        {
            return await this.dataContext
                .PullDogRepositories
                .Include(x => x.PullDogSettings)
                .ThenInclude(x => x.Repositories)
                .Include(x => x.PullDogSettings)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    x => x.Handle == request.RepositoryHandle, 
                    cancellationToken);
        }
    }
}
