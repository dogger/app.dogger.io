using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Queries.PullDog.GetRepositoriesForUser;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetRepositoriesForUserQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoPullDogSettingsFoundOnUser_ThrowsException()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var userId = Guid.NewGuid();
            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithPullDogSettings(null)));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(
                    new GetRepositoriesForUserQuery(userId)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SettingsContainNoRepositories_ThrowsException()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var userId = Guid.NewGuid();
            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithPullDogSettings()
                    .WithId(userId)));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(new GetRepositoriesForUserQuery(userId)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SettingsContainNoInstallationIdOnAnyRepositories_ThrowsException()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var userId = Guid.NewGuid();
            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithRepositories(new TestPullDogRepositoryBuilder().Build()))));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(new GetRepositoriesForUserQuery(userId)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
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

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeGitHubClientFactory)
            });

            var userId = Guid.NewGuid();
            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithRepositories(
                            new TestPullDogRepositoryBuilder()
                                .WithHandle("3")
                                .WithGitHubInstallationId(1337),
                            new TestPullDogRepositoryBuilder()
                                .WithHandle("4")
                                .WithGitHubInstallationId(1337)))));

            //Act
            var repositories = await environment.Mediator.Send(
                new GetRepositoriesForUserQuery(userId),
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
        [TestCategory(TestCategories.IntegrationCategory)]
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

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeGitHubClientFactory)
            });

            var userId = Guid.NewGuid();
            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(userId)
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithRepositories(new TestPullDogRepositoryBuilder()
                            .WithHandle("2")
                            .WithGitHubInstallationId(1337)))));

            //Act
            var repositories = await environment.Mediator.Send(
                new GetRepositoriesForUserQuery(userId),
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
