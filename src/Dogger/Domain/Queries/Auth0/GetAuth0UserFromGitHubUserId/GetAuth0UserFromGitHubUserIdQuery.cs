using Auth0.ManagementApi.Models;
using MediatR;

namespace Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId
{
    public class GetAuth0UserFromGitHubUserIdQuery : IRequest<User?>
    {
        public int GitHubUserId { get; }

        public GetAuth0UserFromGitHubUserIdQuery(
            int gitHubUserId)
        {
            this.GitHubUserId = gitHubUserId;
        }
    }
}
