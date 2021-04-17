using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.DeletePullDogRepositoryByGitHubInstallationId
{
    public class DeletePullDogRepositoryByGitHubInstallationIdCommandHandler : IRequestHandler<DeletePullDogRepositoryByGitHubInstallationIdCommand>
    {
        private readonly DataContext dataContext;
        private readonly IMediator mediator;

        public DeletePullDogRepositoryByGitHubInstallationIdCommandHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<Unit> Handle(DeletePullDogRepositoryByGitHubInstallationIdCommand request, CancellationToken cancellationToken)
        {
            var repository = await this.dataContext
                .PullDogRepositories
                .Include(x => x.PullRequests)
                .SingleOrDefaultAsync(
                    x => x.GitHubInstallationId == request.GitHubInstallationId,
                    cancellationToken);
            if (repository == null)
                return Unit.Value;

            await this.mediator.Send(
                new DeletePullDogRepositoryCommand(repository.Handle),
                cancellationToken);

            return Unit.Value;
        }
    }
}
