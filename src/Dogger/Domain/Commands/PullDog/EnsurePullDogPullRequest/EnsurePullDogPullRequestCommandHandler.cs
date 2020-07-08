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
            await this.dataContext
                .PullDogPullRequests
                .Upsert(new PullDogPullRequest()
                {
                    Handle = request.PullRequestHandle,
                    PullDogRepositoryId = request.Repository.Id
                })
                .On(x => new
                {
                    x.Handle,
                    x.PullDogRepositoryId
                })
                .NoUpdate()
                .AllowIdentityMatch()
                .RunAsync(cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            var pullRequest = await this.dataContext
                .PullDogPullRequests
                .Include(x => x.Instance)
                .Include(x => x.PullDogRepository)
                .ThenInclude(x => x.PullDogSettings)
                .ThenInclude(x => x.User)
                .SingleAsync(
                    pr => 
                        pr.Handle == request.PullRequestHandle &&
                        pr.PullDogRepository == request.Repository,
                    cancellationToken);
            return pullRequest;
        }
    }
}
