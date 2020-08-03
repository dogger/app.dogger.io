using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using Dogger.Infrastructure.Auth.Auth0;
using Dogger.Infrastructure.Ioc;
using MediatR;

namespace Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId
{
    public class GetAuth0UserFromGitHubUserIdQueryHandler : IRequestHandler<GetAuth0UserFromGitHubUserIdQuery, User?>
    {
        private readonly IManagementApiClientFactory? managementApiClientFactory;

        public GetAuth0UserFromGitHubUserIdQueryHandler(
            IOptionalService<IManagementApiClientFactory> managementApiClientFactory)
        {
            this.managementApiClientFactory = managementApiClientFactory.Value;
        }

        public async Task<User?> Handle(GetAuth0UserFromGitHubUserIdQuery request, CancellationToken cancellationToken)
        {
            if (this.managementApiClientFactory == null)
                return null;

            if(request.GitHubUserId == default)
                throw new InvalidOperationException("No GitHub ID is given.");

            using var client = await managementApiClientFactory.CreateAsync();

            var users = await client.GetAllUsersAsync(
                new GetUsersRequest()
                {
                    SearchEngine = "v3",
                    Query = $"user_metadata.dogger_github_user_id: {request.GitHubUserId}"
                },
                new PaginationInfo(0, 1, true));
            return users.SingleOrDefault();
        }
    }
}
