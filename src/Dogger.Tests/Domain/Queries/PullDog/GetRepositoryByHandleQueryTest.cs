using System.Threading.Tasks;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Queries.PullDog
{
    [TestClass]
    public class GetRepositoryByHandleQueryTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoRepositoryForHandleFound_ReturnsNull()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogRepositories.CountAsync());
            });

            //Act
            var repository = await environment.Mediator.Send(new GetRepositoryByHandleQuery("some-repository-handle"));

            //Assert
            Assert.IsNull(repository);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_RepositoryForHandleFound_ReturnsRepository()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var pullDogRepository = new TestPullDogRepositoryBuilder()
                .WithHandle("some-repository-handle")
                .Build();
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(pullDogRepository);
            });

            //Act
            var repository = await environment.Mediator.Send(new GetRepositoryByHandleQuery("some-repository-handle"));

            //Assert
            Assert.IsNotNull(repository);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogRepositories.CountAsync());
            });
        }
    }
}
