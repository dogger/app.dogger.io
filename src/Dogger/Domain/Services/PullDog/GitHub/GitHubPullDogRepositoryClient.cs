using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Octokit;

namespace Dogger.Domain.Services.PullDog.GitHub
{
    public class GitHubPullDogRepositoryClient : IPullDogRepositoryClient
    {
        private readonly IGitHubClient gitHubClient;
        private readonly GitReference gitReference;

        public GitHubPullDogRepositoryClient(
            IGitHubClient gitHubClient,
            GitReference gitReference)
        {
            this.gitHubClient = gitHubClient;
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

                var contents = await GetRepositoryContentsAsync(
                    path, 
                    owner, 
                    name, 
                    reference);
                var resultingFiles = contents
                    .Where(content => content.Type.Value == ContentType.File)
                    .Select(content => new RepositoryFile(
                        content.Path,
                        content.EncodedContent == null ? 
                            Array.Empty<byte>() :
                            Convert.FromBase64String(content.EncodedContent)))
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
                        if (file.Contents.Length > 0)
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
                        file.Contents = bytes;

                        return file;
                    }));
            }
            catch (NotFoundException)
            {
                return Array.Empty<RepositoryFile>();
            }
        }

        private async Task<IReadOnlyList<RepositoryContent>> GetRepositoryContentsAsync(
            string path, 
            string owner, 
            string name, 
            string reference)
        {
            if (string.IsNullOrEmpty(path))
            {
                return await this.gitHubClient
                    .Repository
                    .Content
                    .GetAllContentsByRef(
                        owner,
                        name,
                        reference);
            }
            else
            {
                return await this.gitHubClient
                    .Repository
                    .Content
                    .GetAllContentsByRef(
                        owner,
                        name,
                        path,
                        reference);
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
                $"[{repository.FullName}: PR #{pullRequest.Handle}](https://github.com/{repository.FullName}/pulls?q=is%3Apr+{pullRequest.Handle})");
        }

        public Uri? GetTestEnvironmentListUrl(ConfigurationFile configuration)
        {
            var label = configuration.Label;
            if (label == null)
                return null;

            var repository = GetTargetRepository();
            return new Uri($"https://github.com/{repository.FullName}/pulls?q=is%3Aopen+is%3Apr+label%3A{label}");
        }
    }
}
