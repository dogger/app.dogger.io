using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Dogger.Infrastructure;
using Dogger.Infrastructure.Docker.Yml;
using Serilog;

namespace Dogger.Domain.Services.PullDog
{
    public class PullDogFileCollector : IPullDogFileCollector
    {
        private readonly IPullDogRepositoryClient client;
        private readonly IDockerComposeParserFactory dockerComposeParserFactory;
        private readonly ILogger logger;

        public PullDogFileCollector(
            IPullDogRepositoryClient client,
            IDockerComposeParserFactory dockerComposeParserFactory,
            ILogger logger)
        {
            this.client = client;
            this.dockerComposeParserFactory = dockerComposeParserFactory;
            this.logger = logger;
        }

        public async Task<RepositoryFile[]?> GetRepositoryFilesFromConfiguration(ConfigurationFile configuration)
        {
            var dockerComposeFileContents = await GetDockerComposeYmlContentsFromRepositoryAsync(configuration
                .DockerComposeYmlFilePaths
                .ToArray());
            if (dockerComposeFileContents.Length == 0)
                return null;

            var dockerComposeYmlDirectoryPath =
                Path.GetDirectoryName(configuration
                    .DockerComposeYmlFilePaths
                    .First()) ??
                string.Empty;

            var paths = new HashSet<string>()
            {
                ".env"
            };

            var allDockerfilePaths = new HashSet<string>();
            foreach (var dockerComposeYmlContent in dockerComposeFileContents)
            {
                var parser = this.dockerComposeParserFactory.Create(dockerComposeYmlContent);

                foreach (var path in parser.GetVolumePaths())
                    paths.Add(path);

                foreach (var path in parser.GetEnvironmentFilePaths())
                    paths.Add(path);

                foreach (var path in parser.GetDockerfilePaths())
                    allDockerfilePaths.Add(path);
            }

            if (allDockerfilePaths.Count > 0)
                paths.Add(string.Empty);

            foreach (var path in allDockerfilePaths)
                paths.Add(path);

            foreach (var dockerComposeYmlFilePath in configuration.DockerComposeYmlFilePaths)
            {
                paths.Add(MakePathRelativeToDockerComposeContext(
                    dockerComposeYmlFilePath,
                    dockerComposeYmlDirectoryPath));
            }

            logger.Debug("Will collect {@FilePaths} from the repository.", paths.ToArray());

            var pathContents = await GetFilesFromPathsAsync(
                dockerComposeYmlDirectoryPath,
                paths.Select(path => Path.Join(
                    dockerComposeYmlDirectoryPath,
                    path)));

            return pathContents
                .ToArray();
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
                    var path = MakePathRelativeToDockerComposeContext(
                        file.Path, 
                        dockerComposeYmlFolderPath);

                    return new RepositoryFile(
                        path,
                        file.Contents);
                })
                .ToArray();
        }

        private static string MakePathRelativeToDockerComposeContext(string path, string dockerComposeYmlFolderPath)
        {
            if (path.StartsWith(dockerComposeYmlFolderPath, StringComparison.InvariantCultureIgnoreCase))
            {
                path = path
                    .Substring(dockerComposeYmlFolderPath.Length)
                    .TrimStart(
                        Path.DirectorySeparatorChar,
                        Path.AltDirectorySeparatorChar);
            }

            return path;
        }

        private async Task<string[]> GetDockerComposeYmlContentsFromRepositoryAsync(
            string[] dockerComposeYmlFilePaths)
        {
            var contents = await Task.WhenAll(dockerComposeYmlFilePaths
                .Select(this.client.GetFilesForPathAsync));
            return contents
                .SelectMany(files => files)
                .Select(file => Encoding
                    .UTF8
                    .GetString(
                        file.Contents))
                .ToArray();
        }
    }
}
