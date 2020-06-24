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
                .Where(x => 
                    x.PullDogPullRequest!.PullDogRepository.Handle == request.RepositoryHandle &&
                    x.PullDogPullRequest!.Handle == request.PullRequestHandle)
                .SingleOrDefaultAsync(cancellationToken);
            if (instance == null)
                return Unit.Value;

            await mediator.Send(
                new DeleteInstanceByNameCommand(instance.Name),
                cancellationToken);

            return Unit.Value;
        }
    }
}
