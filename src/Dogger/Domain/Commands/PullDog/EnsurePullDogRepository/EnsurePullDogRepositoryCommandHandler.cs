using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Models.Builders;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.EnsurePullDogRepository
{
    public class EnsurePullDogRepositoryCommandHandler : IRequestHandler<EnsurePullDogRepositoryCommand, PullDogRepository>
    {
        private readonly IMediator mediator;
        private readonly DataContext dataContext;

        public EnsurePullDogRepositoryCommandHandler(
            IMediator mediator,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<PullDogRepository> Handle(EnsurePullDogRepositoryCommand request, CancellationToken cancellationToken)
        {
            var existingRepository = await this.mediator.Send(
                new GetRepositoryByHandleQuery(request.RepositoryHandle),
                cancellationToken);
            if (existingRepository != null)
                return existingRepository;

            var newRepository = new PullDogRepositoryBuilder()
                .WithPullDogSettings(request.PullDogSettings)
                .WithHandle(request.RepositoryHandle)
                .Build();

            await this.dataContext.PullDogRepositories.AddAsync(newRepository, cancellationToken);
            await this.dataContext.SaveChangesAsync(cancellationToken);

            return newRepository;
        }
    }
}
