using System;
using System.Threading.Tasks;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetRepositoryByHandle;
using Dogger.Tests.TestHelpers;
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var pullDogRepository = new PullDogRepository()
            {
                Handle = "some-repository-handle",
                PullDogSettings = new PullDogSettings()
                {
                    User = new User()
                    {
                        StripeCustomerId = "dummy"
                    },
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
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
