using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest
{
    public class EnsurePullDogPullRequestCommandHandler : IRequestHandler<EnsurePullDogPullRequestCommand, PullDogPullRequest>
    {
        private readonly DataContext dataContext;

        public EnsurePullDogPullRequestCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<PullDogPullRequest> Handle(EnsurePullDogPullRequestCommand request, CancellationToken cancellationToken)
        {
            var existingPullRequest = await this.dataContext
                .PullDogPullRequests
                .Include(x => x.Instance)
                .Include(x => x.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(
                    pullRequest => 
                        pullRequest.Handle == request.PullRequestHandle &&
                        pullRequest.PullDogRepository == request.Repository,
                    cancellationToken);
            if (existingPullRequest != null)
                return existingPullRequest;
            
            var newPullRequest = new PullDogPullRequest()
            {
                Handle = request.PullRequestHandle,
                PullDogRepository = request.Repository
            };

            await this.dataContext.PullDogPullRequests.AddAsync(newPullRequest, cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return newPullRequest;
        }
    }
}
