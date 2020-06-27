using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser
{
    public class DeleteAllPullDogInstancesForUserCommandHandler : IRequestHandler<DeleteAllPullDogInstancesForUserCommand>
    {
        private readonly DataContext dataContext;
        private readonly IMediator mediator;

        public DeleteAllPullDogInstancesForUserCommandHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(DeleteAllPullDogInstancesForUserCommand request, CancellationToken cancellationToken)
        {
            var instancesToDelete = await this.dataContext
                .Instances
                .Where(x =>
                    x.PullDogPullRequest != null &&
                    x.PullDogPullRequest!.PullDogRepository.PullDogSettings.UserId == request.UserId)
                .ToArrayAsync(cancellationToken);
            foreach (var instance in instancesToDelete)
            {
                await mediator.Send(
                    new DeleteInstanceByNameCommand(instance.Name),
                    cancellationToken);
            }

            return Unit.Value;
        }
    }
}
