using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class EnsurePullDogRepositoryCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingPullDogRepositoryFound_ReturnsExistingRepository()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var pullDogSettings = new PullDogSettings()
            {
                User = new User()
                {
                    StripeCustomerId = "dummy"
                },
                PlanId = "dummy",
                EncryptedApiKey = Array.Empty<byte>()
            };
            var pullDogRepository = new PullDogRepository()
            {
                Handle = "some-repository-handle",
                PullDogSettings = pullDogSettings
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(pullDogRepository);
            });

            //Act
            var repository = await environment.Mediator.Send(new EnsurePullDogRepositoryCommand(
                pullDogSettings,
                "some-repository-handle"));

            //Assert
            Assert.IsNotNull(repository);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogRepositories.CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoPullDogRepositoryFound_CreatesNewRepository()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            //Act
            var repository = await environment.Mediator.Send(new EnsurePullDogRepositoryCommand(
                new PullDogSettings()
                {
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                },
                "some-repository-handle"));

            //Assert
            Assert.IsNotNull(repository);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogRepositories.CountAsync());
            });
        }
    }
}
