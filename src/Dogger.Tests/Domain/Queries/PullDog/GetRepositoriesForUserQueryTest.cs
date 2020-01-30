using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetRepositoriesForUser;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.TestHelpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using User = Dogger.Domain.Models.User;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetRepositoriesForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_NoPullDogSettingsFoundOnUser_ThrowsException()
        {
            //Arrange
            var handler = new GetRepositoriesForUserQueryHandler(Substitute.For<IGitHubClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await handler.Handle(
                    new GetRepositoriesForUserQuery(new User()
                    {
                        PullDogSettings = null
                    }),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_SettingsContainNoInstallationId_ThrowsException()
        {
            //Arrange
            var handler = new GetRepositoriesForUserQueryHandler(Substitute.For<IGitHubClientFactory>());

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await handler.Handle(
                    new GetRepositoriesForUserQuery(new User()
                    {
                        PullDogSettings = new PullDogSettings()
                        {
                            GitHubInstallationId = null
                        } 
                    }),
                    default));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DatabaseRepositoriesAndGitHubRepositoriesFoundWithNoOverlap_IncludesAllRepositories()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .GitHubApps
                .Installation
                .GetAllRepositoriesForCurrent()
                .Returns(new RepositoriesResponse(
                    2, 
                    new[]
                    {
                        new Repository(1),
                        new Repository(2)
                    }));

            var handler = new GetRepositoriesForUserQueryHandler(fakeGitHubClientFactory);

            //Act
            var repositories = await handler.Handle(
                new GetRepositoriesForUserQuery(new User()
                {
                    PullDogSettings = new PullDogSettings()
                    {
                        GitHubInstallationId = 1337,
                        Repositories = new List<PullDogRepository>()
                        {
                            new PullDogRepository()
                            {
                                Handle = "3"
                            },
                            new PullDogRepository()
                            {
                                Handle = "4"
                            }
                        }
                    }
                }),
                default);

            //Assert
            Assert.IsNotNull(repositories);
            Assert.AreEqual(4, repositories.Count);

            Assert.IsTrue(repositories.Any(x => x.Handle == "1" && x.PullDogId == null));
            Assert.IsTrue(repositories.Any(x => x.Handle == "2" && x.PullDogId == null));
            Assert.IsTrue(repositories.Any(x => x.Handle == "3" && x.PullDogId != null));
            Assert.IsTrue(repositories.Any(x => x.Handle == "4" && x.PullDogId != null));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Handle_DatabaseRepositoriesAndGitHubRepositoriesFoundWithOverlap_MergesRepositories()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubClient
                .GitHubApps
                .Installation
                .GetAllRepositoriesForCurrent()
                .Returns(new RepositoriesResponse(
                    2,
                    new[]
                    {
                        new Repository(1),
                        new Repository(2),
                        new Repository(3)
                    }));

            var handler = new GetRepositoriesForUserQueryHandler(fakeGitHubClientFactory);

            //Act
            var repositories = await handler.Handle(
                new GetRepositoriesForUserQuery(new User()
                {
                    PullDogSettings = new PullDogSettings()
                    {
                        GitHubInstallationId = 1337,
                        Repositories = new List<PullDogRepository>()
                        {
                            new PullDogRepository()
                            {
                                Handle = "2",
                                Id = Guid.NewGuid()
                            }
                        }
                    }
                }),
                default);

            //Assert
            Assert.IsNotNull(repositories);
            Assert.AreEqual(3, repositories.Count);

            Assert.IsTrue(repositories.Any(x => x.Handle == "1" && x.PullDogId == null));
            Assert.IsTrue(repositories.Any(x => x.Handle == "2" && x.PullDogId != null));
            Assert.IsTrue(repositories.Any(x => x.Handle == "3" && x.PullDogId == null));
        }
    }
}
