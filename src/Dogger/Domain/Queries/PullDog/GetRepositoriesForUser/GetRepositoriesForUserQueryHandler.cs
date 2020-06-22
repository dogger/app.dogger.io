using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Infrastructure.GitHub;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.PullDog.GetRepositoriesForUser
{
    public class GetRepositoriesForUserQueryHandler : IRequestHandler<GetRepositoriesForUserQuery, IReadOnlyCollection<UserRepositoryResponse>>
    {
        private readonly IGitHubClientFactory gitHubClientFactory;
        private readonly DataContext dataContext;

        public GetRepositoriesForUserQueryHandler(
            IGitHubClientFactory gitHubClientFactory,
            DataContext dataContext)
        {
            this.gitHubClientFactory = gitHubClientFactory;
            this.dataContext = dataContext;
        }

        public async Task<IReadOnlyCollection<UserRepositoryResponse>> Handle(
            GetRepositoriesForUserQuery request, 
            CancellationToken cancellationToken)
        {
            var databaseRepositories = await dataContext
                .PullDogRepositories
                .Where(x => x.PullDogSettings.UserId == request.UserId)
                .ToListAsync(cancellationToken);

            var allInstallationIds = databaseRepositories
                .GroupBy(x => x.GitHubInstallationId!)
                .Select(x => x.Key)
                .Where(x => x != null)
                .Select(x => x!.Value)
                .ToArray();

            if (allInstallationIds.Length == 0)
                throw new InvalidOperationException("Could not find a GitHub installation ID for the user among the user's repositories.");

            var clients = await Task.WhenAll(allInstallationIds
                .Select(installationId => gitHubClientFactory
                    .CreateInstallationClientAsync(installationId)));

            var gitHubRepositoryResponses = await Task.WhenAll(clients
                .Select(client => client
                    .GitHubApps
                    .Installation
                    .GetAllRepositoriesForCurrent()));

            var mappedGitHubRepositories = gitHubRepositoryResponses
                .SelectMany(x => x.Repositories)
                .Select(x => new UserRepositoryResponse()
                {
                    Handle = x.Id.ToString(CultureInfo.InvariantCulture),
                    Name = x.Name,
                    PullDogId = null
                })
                .ToList();

            var mappedPullDogRepositories = databaseRepositories
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
