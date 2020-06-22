using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories
{
    public class AddPullDogToGitHubRepositoriesCommandHandler : IRequestHandler<AddPullDogToGitHubRepositoriesCommand>
    {
        private readonly IMediator mediator;
        private readonly DataContext dataContext;

        public AddPullDogToGitHubRepositoriesCommandHandler(
            IMediator mediator,
            DataContext dataContext)
        {
            this.mediator = mediator;
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(AddPullDogToGitHubRepositoriesCommand request, CancellationToken cancellationToken)
        {
            foreach (var repositoryId in request.GitHubRepositoryIds)
            {
                var pullDogRepository = await this.mediator.Send(
                    new EnsurePullDogRepositoryCommand(
                        request.PullDogSettings,
                        repositoryId.ToString(CultureInfo.InvariantCulture)),
                    cancellationToken);
                pullDogRepository.GitHubInstallationId = request.GitHubInstallationId;
            }

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
