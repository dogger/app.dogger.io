using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetPullDogSettingsByGitHubInstallationIdQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoMatchingSettings_ReturnsNull()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubInstallationIdQuery(1337));

            //Assert
            Assert.IsNull(settings);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SeveralSettings_ReturnsMatchingSettings()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1336)
                        .Build())
                    .Build());
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)
                        .Build())
                    .Build());
                await dataContext.PullDogSettings.AddAsync(new TestPullDogSettingsBuilder()
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1338)
                        .Build())
                    .Build());
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubInstallationIdQuery(1337));

            //Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(1337, settings.Repositories.Single().GitHubInstallationId);
        }
    }
}
