using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace Dogger.Tests.Domain.Services.PullDog
{
    [TestClass]
    public class PullDogFileCollectorTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetConfigurationFile_FilePresent_ReturnsDeserializedFile()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(new[]
                {
                    new RepositoryFile(
                        "pull-dog.json",
                        Encoding.UTF8.GetBytes("{}"))
                });

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>(),
                Substitute.For<ILogger>());

            //Act
            var configurationFile = await client.GetConfigurationFileAsync();

            //Assert
            Assert.IsNotNull(configurationFile);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetConfigurationFile_FileNotPresent_ReturnsNull()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(Array.Empty<RepositoryFile>());

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>(),
                Substitute.For<ILogger>());

            //Act
            var configurationFile = await client.GetConfigurationFileAsync();

            //Assert
            Assert.IsNull(configurationFile);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFilesFromConfiguration_ConfigurationFileNotPresent_ReturnsNull()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(Array.Empty<RepositoryFile>());

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>(),
                Substitute.For<ILogger>());

            //Act
            var composeContents = await client.GetRepositoryFilesFromConfiguration(new ConfigurationFile(Array.Empty<string>()));

            //Assert
            Assert.IsNull(composeContents);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFilesFromConfiguration_NoDockerComposeFilePathsPresent_ReturnsNull()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(new[] {
                    new RepositoryFile(
                        "pull-dog.json",
                        Encoding.UTF8.GetBytes("{}"))
                });

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>(),
                Substitute.For<ILogger>());

            //Act
            var dockerComposeYmlContents = await client.GetRepositoryFilesFromConfiguration(new ConfigurationFile(Array.Empty<string>()));

            //Assert
            Assert.IsNull(dockerComposeYmlContents);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFilesFromConfiguration_DockerComposeYmlWithOneOfEachPathTypeAndRelativeDirectoryGiven_ReturnsProperPaths()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync(Path.Join("relative", "dir", "some-docker-compose.yml"))
                .Returns(new[]
                {
                    new RepositoryFile(
                        "dummy",
                        Encoding.UTF8.GetBytes("some-docker-compose-contents"))
                });

            fakePullDogRepositoryClient
                .GetFilesForPathAsync(Path.Join("relative", "dir", "some-volume-path"))
                .Returns(new[]
                {
                    new RepositoryFile(
                        "dummy",
                        Encoding.UTF8.GetBytes("dummy"))
                });

            fakePullDogRepositoryClient
                .GetFilesForPathAsync(Path.Join("relative", "dir", "some-environment-file-path"))
                .Returns(new[]
                {
                    new RepositoryFile(
                        "dummy",
                        Encoding.UTF8.GetBytes("dummy"))
                });

            fakePullDogRepositoryClient
                .GetFilesForPathAsync(Path.Join("relative", "dir", "some-dockerfile-path"))
                .Returns(new[]
                {
                    new RepositoryFile(
                        "dummy",
                        Encoding.UTF8.GetBytes("dummy"))
                });

            fakePullDogRepositoryClient
                .GetFilesForPathAsync(Path.Join("relative", "dir", "some-additional-path"))
                .Returns(new[]
                {
                    new RepositoryFile(
                        "dummy",
                        Encoding.UTF8.GetBytes("dummy"))
                });

            var fakeDockerComposeParserFactory = Substitute.For<IDockerComposeParserFactory>();

            var fakeDockerComposeParser = fakeDockerComposeParserFactory
                .Create("some-docker-compose-contents");

            fakeDockerComposeParser
                .GetVolumePaths()
                .Returns(new[]
                {
                    "some-volume-path"
                });

            fakeDockerComposeParser
                .GetEnvironmentFilePaths()
                .Returns(new[]
                {
                    "some-environment-file-path"
                });

            fakeDockerComposeParser
                .GetDockerfilePaths()
                .Returns(new[]
                {
                    "some-dockerfile-path"
                });

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                fakeDockerComposeParserFactory,
                Substitute.For<ILogger>());

            //Act
            var files = await client.GetRepositoryFilesFromConfiguration(
                new ConfigurationFile(new[]
                {
                    Path.Join("relative", "dir", "some-docker-compose.yml")
                }));

            //Assert
            Assert.AreEqual(3, files.Length);
        }
    }
}
