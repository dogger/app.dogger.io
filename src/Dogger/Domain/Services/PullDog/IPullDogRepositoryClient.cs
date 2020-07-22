using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;

namespace Dogger.Domain.Services.PullDog
{
    public interface IPullDogRepositoryClient
    {
        Task<RepositoryFile[]> GetFilesForPathAsync(string path);
        PullRequestDetails GetPullRequestDetails(PullDogPullRequest pullRequest);

        Uri? GetTestEnvironmentListUrl(ConfigurationFile configuration);
    }

}
