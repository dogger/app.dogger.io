using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
using Dogger.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    GitHubInstallationId = 1336,
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>()
                });
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    GitHubInstallationId = 1337,
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>()
                });
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    GitHubInstallationId = 1338,
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>()
                });
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubInstallationIdQuery(1337));

            //Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(1337, settings.GitHubInstallationId);
        }
    }
}
