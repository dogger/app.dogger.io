using System.Threading.Tasks;
using Dogger.Domain.Models;

namespace Dogger.Domain.Services.PullDog
{
    public interface IPullDogRepositoryClient
    {
        Task<RepositoryFile[]> GetFilesForPathAsync(string path);
        PullRequestDetails GetPullRequestDetails(PullDogPullRequest pullRequest);
    }

    public class PullRequestDetails
    {
        public string RepositoryFullName { get; }
        public string PullRequestLink { get; }

        public PullRequestDetails(
            string repositoryFullName,
            string pullRequestLink)
        {
            this.RepositoryFullName = repositoryFullName;
            this.PullRequestLink = pullRequestLink;
        }
    }
}
