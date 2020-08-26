using Dogger.Domain.Models;
using MediatR;

namespace Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubPayloadInformation
{
    public class GetPullDogSettingsByGitHubPayloadInformationQuery : IRequest<PullDogSettings?>
    {
        public long InstallationId { get; }
        public int UserId { get; }

        public GetPullDogSettingsByGitHubPayloadInformationQuery(
            long installationId,
            int userId)
        {
            this.InstallationId = installationId;
            this.UserId = userId;
        }
    }
}
