using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Docker.Yml;

namespace Dogger.Domain.Services.PullDog
{

    public class PullDogFileCollector : IPullDogFileCollector
    {
        private readonly IPullDogRepositoryClient client;
        private readonly IDockerComposeParserFactory dockerComposeParserFactory;

        public PullDogFileCollector(
            IPullDogRepositoryClient client,
            IDockerComposeParserFactory dockerComposeParserFactory)
        {
            this.client = client;
            this.dockerComposeParserFactory = dockerComposeParserFactory;
        }

        public async Task<RepositoryPullDogFileContext?> GetRepositoryFileContextFromConfiguration(ConfigurationFile configuration)
        {
            var dockerComposeFileContents = await GetDockerComposeYmlContentsFromRepositoryAsync(configuration);
            if (dockerComposeFileContents == null)
                return null;

            var dockerComposeYmlDirectoryPath = Path.GetDirectoryName(configuration
                .DockerComposeYmlFilePaths
                .First()) ?? string.Empty;

            var allFiles = new List<RepositoryFile>();

            var dockerFiles = await GetAllDockerFilesFromComposeContentsAsync(
                dockerComposeYmlDirectoryPath,
                dockerComposeFileContents);
            allFiles.AddRange(dockerFiles);

            if (configuration.AdditionalPaths != null)
            {
                var additionalFiles = await GetFilesFromPathsAsync(
                    dockerComposeYmlDirectoryPath,
                    configuration.AdditionalPaths);
                allFiles.AddRange(additionalFiles);
            }

            return new RepositoryPullDogFileContext(
                dockerComposeFileContents,
                allFiles.ToArray());
        }

        public async Task<ConfigurationFile?> GetConfigurationFileAsync()
        {
            var configurationFiles = await this.client.GetFilesForPathAsync("pull-dog.json");
            var configurationFileEntry = configurationFiles.SingleOrDefault();
            if (configurationFileEntry == null)
                return null;

            var configurationFile = JsonSerializer.Deserialize<ConfigurationFile>(
                configurationFileEntry.Contents,
                JsonFactory.GetOptions());
            return configurationFile;
        }

        private async Task<RepositoryFile[]> GetAllDockerFilesFromComposeContentsAsync(
            string dockerComposeYmlDirectoryPath,
            string[] dockerComposeYmlContents)
        {
            var paths = new List<string>()
            {
                ".env"
            };

            foreach (var dockerComposeYmlContent in dockerComposeYmlContents)
            {
                var parser = this.dockerComposeParserFactory.Create(dockerComposeYmlContent);
                paths.AddRange(parser.GetVolumePaths());
                paths.AddRange(parser.GetEnvironmentFilePaths());
                paths.AddRange(parser.GetDockerfilePaths());
            }

            var pathContents = await GetFilesFromPathsAsync(
                dockerComposeYmlDirectoryPath,
                paths.Select(path => Path.Join(
                    dockerComposeYmlDirectoryPath,
                    path)));

            return pathContents
                .ToArray();
        }

        private async Task<RepositoryFile[]> GetFilesFromPathsAsync(
            string dockerComposeYmlFolderPath,
            IEnumerable<string> paths)
        {
            var files = await Task.WhenAll(paths
                .Distinct()
                .Select(this
                    .client
                    .GetFilesForPathAsync));
            return files
                .SelectMany(x => x)
                .Select(file =>
                {
                    var path = file.Path;
                    if (path.StartsWith(dockerComposeYmlFolderPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        path = path
                            .Substring(dockerComposeYmlFolderPath.Length)
                            .TrimStart(
                                Path.DirectorySeparatorChar,
                                Path.AltDirectorySeparatorChar);
                    }

                    return new RepositoryFile(
                        path,
                        file.Contents);
                })
                .ToArray();
        }

        private async Task<string[]?> GetDockerComposeYmlContentsFromRepositoryAsync(ConfigurationFile configurationFile)
        {
            if (configurationFile.DockerComposeYmlFilePaths == null)
                return null;

            var contents = await Task.WhenAll(configurationFile
                .DockerComposeYmlFilePaths
                .Select(this.client.GetFilesForPathAsync));
            return contents
                .SelectMany(files => files)
                .Select(file => file.Contents)
                .ToArray();
        }
    }
}
