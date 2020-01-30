using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId
{
    public class GetPullDogSettingsByGitHubInstallationIdQuery : IRequest<PullDogSettings?>
    {
        public long InstallationId { get; }

        public GetPullDogSettingsByGitHubInstallationIdQuery(
            long installationId)
        {
            this.InstallationId = installationId;
        }
    }
}
