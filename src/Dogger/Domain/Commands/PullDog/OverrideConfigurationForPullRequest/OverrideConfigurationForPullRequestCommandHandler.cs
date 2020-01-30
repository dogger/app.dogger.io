using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest
{
    public class OverrideConfigurationForPullRequestCommandHandler : IRequestHandler<OverrideConfigurationForPullRequestCommand>
    {
        private readonly DataContext dataContext;

        public OverrideConfigurationForPullRequestCommandHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<Unit> Handle(OverrideConfigurationForPullRequestCommand request, CancellationToken cancellationToken)
        {
            var pullRequest = await this.dataContext
                .PullDogPullRequests
                .SingleOrDefaultAsync(
                    x => x.Id == request.PullDogPullRequestId,
                    cancellationToken);
            pullRequest.ConfigurationOverride = request.ConfigurationOverride;

            await this.dataContext.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
