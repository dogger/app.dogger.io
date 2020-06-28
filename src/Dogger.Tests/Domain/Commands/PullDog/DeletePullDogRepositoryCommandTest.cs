using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.DeletePullDogRepository;
using Dogger.Domain.Models;
using Dogger.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class DeletePullDogRepositoryCommandTest
    {
        [TestMethod]
        public async Task Handle_PullDogRepositoryWithPullRequestPresentInDatabase_DeletesBothRepositoryAndPullRequests()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(new PullDogRepository()
                {
                    Handle = "some-handle",
                    PullDogSettings = new PullDogSettings()
                    {
                        EncryptedApiKey = Array.Empty<byte>(),
                        PlanId = "dummy",
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        }
                    },
                    PullRequests = new List<PullDogPullRequest>()
                    {
                        new PullDogPullRequest()
                        {
                            Handle = "dummy-1"
                        },
                        new PullDogPullRequest()
                        {
                            Handle = "dummy-2"
                        }
                    }
                });
            });

            Assert.AreEqual(1, await environment.DataContext.PullDogRepositories.CountAsync());
            Assert.AreEqual(2, await environment.DataContext.PullDogPullRequests.CountAsync());

            //Act
            await environment.Mediator.Send(new DeletePullDogRepositoryCommand(
                "some-handle"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogRepositories.CountAsync());
                Assert.AreEqual(0, await dataContext.PullDogPullRequests.CountAsync());
            });
        }

        [TestMethod]
        public async Task Handle_MatchingAndUnmatchingRepositoriesPresent_DeletesMatchingRepository()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogRepositories.AddAsync(new PullDogRepository()
                {
                    Handle = "dummy-1",
                    PullDogSettings = new PullDogSettings()
                    {
                        EncryptedApiKey = Array.Empty<byte>(),
                        PlanId = "dummy",
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        }
                    },
                });

                await dataContext.PullDogRepositories.AddAsync(new PullDogRepository()
                {
                    Handle = "some-handle",
                    PullDogSettings = new PullDogSettings()
                    {
                        EncryptedApiKey = Array.Empty<byte>(),
                        PlanId = "dummy",
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        }
                    },
                });

                await dataContext.PullDogRepositories.AddAsync(new PullDogRepository()
                {
                    Handle = "dummy-2",
                    PullDogSettings = new PullDogSettings()
                    {
                        EncryptedApiKey = Array.Empty<byte>(),
                        PlanId = "dummy",
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        }
                    },
                });
            });

            Assert.AreEqual(3, await environment.DataContext.PullDogRepositories.CountAsync());

            //Act
            await environment.Mediator.Send(new DeletePullDogRepositoryCommand(
                "some-handle"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var repositories = await dataContext
                    .PullDogRepositories
                    .ToListAsync();
                Assert.AreEqual(2, repositories.Count);

                Assert.IsFalse(repositories.Any(r => r.Handle == "some-handle"));
            });
        }
    }
}
