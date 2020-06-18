using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.DeletePullDogRepository
{
    public class DeletePullDogRepositoryCommandHandler : IRequestHandler<DeletePullDogRepositoryCommand>
    {
        private readonly DataContext dataContext;

        public DeletePullDogRepositoryCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(DeletePullDogRepositoryCommand request, CancellationToken cancellationToken)
        {
            var repository = await this.dataContext
                .PullDogRepositories
                .Include(x => x.PullRequests)
                .SingleOrDefaultAsync(
                    x => x.Handle == request.Handle,
                    cancellationToken);
            if (repository == null)
                return Unit.Value;

            this.dataContext.PullDogRepositories.Remove(repository);

            foreach (var pullRequest in repository.PullRequests)
                this.dataContext.PullDogPullRequests.Remove(pullRequest);

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
