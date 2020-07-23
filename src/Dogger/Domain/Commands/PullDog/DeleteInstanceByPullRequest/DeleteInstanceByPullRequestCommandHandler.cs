using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.DeleteInstanceByPullRequest
{
    public class DeleteInstanceByPullRequestCommandHandler : IRequestHandler<DeleteInstanceByPullRequestCommand>
    {
        private readonly IMediator mediator;

        private readonly DataContext dataContext;

        public DeleteInstanceByPullRequestCommandHandler(
            IMediator mediator,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(DeleteInstanceByPullRequestCommand request, CancellationToken cancellationToken)
        {
            var pullRequest = await this.dataContext
                .PullDogPullRequests
                .Include(x => x.Instance)
                .Where(x => 
                    x.PullDogRepository.Handle == request.RepositoryHandle &&
                    x.Handle == request.PullRequestHandle)
                .SingleOrDefaultAsync(cancellationToken);
            if (pullRequest == null)
                return Unit.Value;

            var instance = pullRequest.Instance;
            if (instance != null)
            {
                await mediator.Send(
                    new DeleteInstanceByNameCommand(
                        instance.Name,
                        request.InitiatedBy),
                    cancellationToken);
            }

            this.dataContext.PullDogPullRequests.Remove(pullRequest);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
