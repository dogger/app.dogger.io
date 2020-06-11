using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Octokit;
using Serilog;

namespace Dogger.Domain.Services.PullDog.GitHub
{
    public class GitHubPullDogRepositoryClient : IPullDogRepositoryClient
    {
        private readonly IGitHubClient gitHubClient;
        private readonly ILogger logger;

        private readonly GitReference gitReference;

        public GitHubPullDogRepositoryClient(
            IGitHubClient gitHubClient,
            ILogger logger,
            GitReference gitReference)
        {
            this.gitHubClient = gitHubClient;
            this.logger = logger;
            this.gitReference = gitReference;
        }

        public async Task<RepositoryFile[]> GetFilesForPathAsync(string path)
        {
            var headRepository = GetTargetRepository();

            try
            {
                var owner = headRepository.Owner.Login;
                var name = headRepository.Name;
                var reference = this.gitReference.Ref;

                var contents = await gitHubClient
                    .Repository
                    .Content
                    .GetAllContentsByRef(
                        owner,
                        name,
                        path,
                        reference);
                var resultingFiles = contents
                    .Where(content => content.Type.Value == ContentType.File)
                    .Select(content => new RepositoryFile(
                        content.Path,
                        content.Content))
                    .ToArray();

                var subDirectoryFiles = await Task.WhenAll(contents
                    .Where(content => content.Type.Value == ContentType.Dir)
                    .Select(content => GetFilesForPathAsync(content.Path)));
                var subDirectoryFilesNormalized = subDirectoryFiles
                    .SelectMany(x => x)
                    .ToArray();

                return await Task.WhenAll(resultingFiles
                    .Union(subDirectoryFilesNormalized)
                    .Select(async file =>
                    {
                        if (file.Contents != null)
                            return file;

                        var bytes = await this
                            .gitHubClient
                            .Repository
                            .Content
                            .GetRawContentByRef(
                                owner,
                                name,
                                file.Path,
                                reference);
                        file.Contents = Encoding.UTF8.GetString(bytes);

                        return file;
                    }));
            }
            catch (NotFoundException)
            {
                return Array.Empty<RepositoryFile>();
            }
        }

        private Repository GetTargetRepository()
        {
            var headRepository = this.gitReference.Repository;
            return headRepository;
        }

        public PullRequestDetails GetPullRequestDetails(PullDogPullRequest pullRequest)
        {
            var repository = GetTargetRepository();
            return new PullRequestDetails(
                repository.FullName,
                $"[{repository.FullName}: PR #{pullRequest.Handle}](https://github.com/{repository.FullName}/pulls?q=is%3Apr+{pullRequest.Handle})",
                $"#{pullRequest.Handle}");
        }
    }
}
