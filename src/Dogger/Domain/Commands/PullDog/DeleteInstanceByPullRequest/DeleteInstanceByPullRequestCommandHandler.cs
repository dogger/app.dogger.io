using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Commands.PullDog.UpsertPullRequestComment;
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
            var instance = await this.dataContext
                .Instances
                .Include(x => x.PullDogPullRequest!)
                .ThenInclude(x => x.PullDogRepository!)
                .ThenInclude(x => x.PullDogSettings!)
                .ThenInclude(x => x.User!)
                .Where(x => 
                    x.PullDogPullRequest!.PullDogRepository.Handle == request.RepositoryHandle &&
                    x.PullDogPullRequest!.Handle == request.PullRequestHandle)
                .SingleOrDefaultAsync(cancellationToken);
            if (instance == null)
                return Unit.Value;

            await mediator.Send(
                new DeleteInstanceByNameCommand(instance.Name),
                cancellationToken);

            await this.mediator.Send(
                new UpsertPullRequestCommentCommand(
                    instance.PullDogPullRequest!,
                    "The test environment for this pull request has been destroyed :boom: This may have happened explicitly via a command, or because the pull request was closed."),
                cancellationToken);

            return Unit.Value;
        }
    }
}
