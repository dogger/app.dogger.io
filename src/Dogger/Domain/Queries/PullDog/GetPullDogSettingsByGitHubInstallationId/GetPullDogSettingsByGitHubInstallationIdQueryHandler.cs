using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId
{
    public class GetPullDogSettingsByGitHubInstallationIdQueryHandler : IRequestHandler<GetPullDogSettingsByGitHubInstallationIdQuery, PullDogSettings?>
    {
        private readonly DataContext dataContext;

        public GetPullDogSettingsByGitHubInstallationIdQueryHandler(
            DataContext dataContext)
        {
            this.dataContext = dataContext;
        }

        public async Task<PullDogSettings?> Handle(GetPullDogSettingsByGitHubInstallationIdQuery request, CancellationToken cancellationToken)
        {
            var repository = await this.dataContext
                .PullDogRepositories
                .Include(x => x.PullDogSettings)
                .Where(x => x.GitHubInstallationId == request.InstallationId)
                .FirstOrDefaultAsync(cancellationToken);
            return repository?.PullDogSettings;
        }
    }
}
