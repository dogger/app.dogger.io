using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class DeletePullDogRepositoryCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogRepositoryWithPullRequestPresentInDatabase_DeletesBothRepositoryAndPullRequests()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(new TestPullDogRepositoryBuilder()
                    .WithHandle("some-handle")
                    .WithPullRequests(
                        new TestPullDogPullRequestBuilder(),
                        new TestPullDogPullRequestBuilder().Build()));
            });

            Assert.AreEqual(1, await environment.DataContext.PullDogRepositories.AsQueryable().CountAsync());
            Assert.AreEqual(2, await environment.DataContext.PullDogPullRequests.AsQueryable().CountAsync());

            //Act
            await environment.Mediator.Send(new DeletePullDogRepositoryCommand(
                "some-handle"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogRepositories.AsQueryable().CountAsync());
                Assert.AreEqual(0, await dataContext.PullDogPullRequests.AsQueryable().CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MatchingAndUnmatchingRepositoriesPresent_DeletesMatchingRepository()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(new TestPullDogRepositoryBuilder().Build());

                await dataContext.PullDogRepositories.AddAsync(new TestPullDogRepositoryBuilder()
                    .WithHandle("some-handle"));

                await dataContext.PullDogRepositories.AddAsync(new TestPullDogRepositoryBuilder().Build());
            });

            Assert.AreEqual(3, await environment.DataContext.PullDogRepositories.AsQueryable().CountAsync());

            //Act
            await environment.Mediator.Send(new DeletePullDogRepositoryCommand(
                "some-handle"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var repositories = await dataContext
                    .PullDogRepositories
                    .AsQueryable()
                    .ToListAsync();
                Assert.AreEqual(2, repositories.Count);

                Assert.IsFalse(repositories.Any(r => r.Handle == "some-handle"));
            });
        }
    }
}
