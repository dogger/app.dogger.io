using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetPullDogSettingsByGitHubInstallationId;
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
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>(),
                    Repositories = new List<PullDogRepository>()
                    {
                        new PullDogRepository()
                        {
                            Handle = "dummy",
                            GitHubInstallationId = 1336
                        }
                    }
                });
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>(),
                    Repositories = new List<PullDogRepository>()
                    {
                        new PullDogRepository()
                        {
                            Handle = "dummy",
                            GitHubInstallationId = 1337
                        }
                    }
                });
                await dataContext.PullDogSettings.AddAsync(new PullDogSettings()
                {
                    PlanId = "dummy",
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    EncryptedApiKey = Array.Empty<byte>(),
                    Repositories = new List<PullDogRepository>()
                    {
                        new PullDogRepository()
                        {
                            Handle = "dummy",
                            GitHubInstallationId = 1338
                        }
                    }
                });
            });

            //Act
            var settings = await environment.Mediator.Send(new GetPullDogSettingsByGitHubInstallationIdQuery(1337));

            //Assert
            Assert.IsNotNull(settings);
            Assert.AreEqual(1337, settings.Repositories.Single().GitHubInstallationId);
        }
    }
}
