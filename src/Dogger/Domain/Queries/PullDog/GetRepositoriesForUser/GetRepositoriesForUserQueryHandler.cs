using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Infrastructure.GitHub;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetRepositoriesForUser
{
    public class GetRepositoriesForUserQueryHandler : IRequestHandler<GetRepositoriesForUserQuery, IReadOnlyCollection<UserRepositoryResponse>>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;

        public GetRepositoriesForUserQueryHandler(
            IGitHubClientFactory gitHubClientFactory)
        {
            this.gitHubClientFactory = gitHubClientFactory;
        }

        public async Task<IReadOnlyCollection<UserRepositoryResponse>> Handle(
            GetRepositoriesForUserQuery request, 
            CancellationToken cancellationToken)
        {
            var settings = request.User.PullDogSettings;
            if (settings == null)
                throw new InvalidOperationException("Could not find Pull Dog settings.");

            if (settings.GitHubInstallationId == null)
                throw new InvalidOperationException("Could not find a GitHub installation ID.");

            var client = await gitHubClientFactory.CreateInstallationClientAsync(settings.GitHubInstallationId.Value);
            var gitHubRepositories = await client.GitHubApps.Installation.GetAllRepositoriesForCurrent();

            var mappedGitHubRepositories = gitHubRepositories
                .Repositories
                .Select(x => new UserRepositoryResponse()
                {
                    Handle = x.Id.ToString(CultureInfo.InvariantCulture),
                    Name = x.Name,
                    PullDogId = null
                })
                .ToList();

            var mappedPullDogRepositories = settings
                .Repositories
                .Select(x => new UserRepositoryResponse()
                {
                    Handle = x.Handle,
                    Name = mappedGitHubRepositories
                        .SingleOrDefault(r => r.Handle == x.Handle)
                        ?.Name,
                    PullDogId = x.Id
                })
                .ToArray();

            return mappedPullDogRepositories
                .Union(mappedGitHubRepositories
                    .Where(gh => mappedPullDogRepositories
                        .All(pd => pd.Handle != gh.Handle)))
                .ToArray();
        }
    }
}
