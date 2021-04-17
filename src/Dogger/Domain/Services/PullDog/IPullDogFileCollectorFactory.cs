using System.Threading.Tasks;
using Dogger.Domain.Models;

namespace Dogger.Domain.Services.PullDog
{
    public interface IPullDogFileCollectorFactory
    {
        Task<IPullDogFileCollector?> CreateAsync(PullDogPullRequest pullRequest);
        IPullDogFileCollector Create(IPullDogRepositoryClient client);
    }
}
