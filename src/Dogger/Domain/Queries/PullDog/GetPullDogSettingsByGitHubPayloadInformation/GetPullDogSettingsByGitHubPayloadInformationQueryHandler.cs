using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubPayloadInformation
{
    public class GetPullDogSettingsByGitHubPayloadInformationQueryHandler : IRequestHandler<GetPullDogSettingsByGitHubPayloadInformationQuery, PullDogSettings?>
    {
        private readonly DataContext dataContext;
        private readonly IMediator mediator;

        public GetPullDogSettingsByGitHubPayloadInformationQueryHandler(
            DataContext dataContext,
            IMediator mediator)
        {
            this.dataContext = dataContext;
            this.mediator = mediator;
        }

        public async Task<PullDogSettings?> Handle(GetPullDogSettingsByGitHubPayloadInformationQuery request, CancellationToken cancellationToken)
        {
            var repository = await this.dataContext
                .PullDogRepositories
                .Include(x => x.PullDogSettings)
                .Where(x => x.GitHubInstallationId == request.InstallationId)
                .FirstOrDefaultAsync(cancellationToken);
            if(repository != null)
                return repository.PullDogSettings;

            var auth0User = await this.mediator.Send(
                new GetAuth0UserFromGitHubUserIdQuery(request.UserId),
                cancellationToken);
            if (auth0User?.EmailVerified != true)
                return null;

            var user = await this.mediator.Send(
                new GetUserByIdentityNameQuery(auth0User.UserId),
                cancellationToken);
            if (user == null)
                throw new InvalidOperationException("Could not find the given user.");

            return user.PullDogSettings;
        }
    }
}
