using System.Threading.Tasks;
using Dogger.Domain.Models;

namespace Dogger.Domain.Services.PullDog
{
    public interface IPullDogRepositoryClientFactory
    {
        Task<IPullDogRepositoryClient> CreateAsync(
            PullDogPullRequest pullRequest);
    }
}
