using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
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

            var pullDogSettings = new TestPullDogSettingsBuilder().Build();
            var pullDogRepository = new TestPullDogRepositoryBuilder()
                .WithHandle("some-repository-handle")
                .WithPullDogSettings(pullDogSettings)
                .Build();

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
                new TestPullDogSettingsBuilder().Build(),
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
