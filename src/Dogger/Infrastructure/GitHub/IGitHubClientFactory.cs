using System.Threading.Tasks;
using Octokit;

namespace Dogger.Infrastructure.GitHub
{
    public interface IGitHubClientFactory
    {
        Task<IGitHubClient> CreateInstallationClientAsync(long installationId);
        Task<IGitHubClient> CreateInstallationInitiatorClientAsync(string code);
    }
}
