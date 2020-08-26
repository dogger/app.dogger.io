using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubPayloadInformation;
using Dogger.Domain.Queries.Users.GetUserByIdentityName;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetPullDogSettingsByGitHubPayloadInformationQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoMatchingSettingsByDatabaseNorAuth0_ReturnsNull()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAuth0UserFromGitHubUserIdQuery>(args =>
                    args.GitHubUserId == 1337))
                .Returns((User)null);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubPayloadInformationQuery(
                1337,
                1337));

            //Assert
            Assert.IsNull(settings);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SeveralSettingsInDatabase_ReturnsMatchingSettings()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1336)));
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)));
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1338)));
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubPayloadInformationQuery(
                1337,
                1337));

            //Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(1337, settings.Repositories.Single().GitHubInstallationId);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MatchingAuth0UserByGitHubUserId_ReturnsUserPullDogSettings()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAuth0UserFromGitHubUserIdQuery>(args =>
                    args.GitHubUserId == 1338))
                .Returns(new User()
                {
                    EmailVerified = true,
                    UserId = "some-user-id"
                });

            fakeMediator
                .Send(Arg.Is<GetUserByIdentityNameQuery>(args =>
                    args.IdentityName == "some-user-id"))
                .Returns(new TestUserBuilder()
                    .WithPullDogSettings());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeMediator);
                }
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubPayloadInformationQuery(
                1337,
                1338));

            //Assert
            Assert.IsNotNull(settings);
        }
    }
}
