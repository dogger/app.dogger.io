using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Dogger.Domain.Services.PullDog;
using Dogger.Infrastructure.Docker.Yml;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

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
                Substitute.For<IDockerComposeParserFactory>());

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
                Substitute.For<IDockerComposeParserFactory>());

            //Act
            var configurationFile = await client.GetConfigurationFileAsync();

            //Assert
            Assert.IsNull(configurationFile);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFileContextFromConfiguration_ConfigurationFileNotPresent_ReturnsNull()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(Array.Empty<RepositoryFile>());

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>());

            //Act
            var composeContents = await client.GetRepositoryFileContextFromConfiguration(new ConfigurationFile());

            //Assert
            Assert.IsNull(composeContents);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFileContextFromConfiguration_NoDockerComposeFilePathsPresent_ReturnsNull()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("pull-dog.json")
                .Returns(new [] {
                    new RepositoryFile(
                        "pull-dog.json",
                        Encoding.UTF8.GetBytes("{}"))
                });

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>());

            //Act
            var dockerComposeYmlContents = await client.GetRepositoryFileContextFromConfiguration(new ConfigurationFile());

            //Assert
            Assert.IsNull(dockerComposeYmlContents);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFileContextFromConfiguration_ValidConfigurationWithPaths_ReturnsPathContents()
        {
            //Arrange
            var fakePullDogRepositoryClient = Substitute.For<IPullDogRepositoryClient>();
            fakePullDogRepositoryClient
                .GetFilesForPathAsync("foo")
                .Returns(new[]
                {
                    new RepositoryFile(
                        "foo",
                        Encoding.UTF8.GetBytes("foo-contents"))
                });

            fakePullDogRepositoryClient
                .GetFilesForPathAsync("bar")
                .Returns(new[]
                {
                    new RepositoryFile(
                        "bar",
                        Encoding.UTF8.GetBytes("bar-contents"))
                });

            var client = new PullDogFileCollector(
                fakePullDogRepositoryClient,
                Substitute.For<IDockerComposeParserFactory>());

            //Act
            var context = await client.GetRepositoryFileContextFromConfiguration(new ConfigurationFile()
            {
                DockerComposeYmlFilePaths = new[] { "foo", "bar" }
            });

            //Assert
            Assert.IsNotNull(context);
            Assert.AreEqual(2, context.DockerComposeYmlContents.Length);

            Assert.AreEqual("foo-contents", context.DockerComposeYmlContents[0]);
            Assert.AreEqual("bar-contents", context.DockerComposeYmlContents[1]);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetRepositoryFileContextFromConfiguration_DockerComposeYmlWithOneOfEachPathTypeAndRelativeDirectoryGiven_ReturnsProperPaths()
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
                fakeDockerComposeParserFactory);

            //Act
            var context = await client.GetRepositoryFileContextFromConfiguration(
                new ConfigurationFile()
                {
                    DockerComposeYmlFilePaths = new[]
                    {
                        Path.Join("relative", "dir", "some-docker-compose.yml")
                    }
                });

            //Assert
            Assert.AreEqual(3, context.Files.Length);
        }
    }
}
