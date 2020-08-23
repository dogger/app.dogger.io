﻿using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogPullRequest;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class EnsurePullDogPullRequestCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingPullRequestFound_ReturnsExistingPullRequest()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var pullDogRepository = new PullDogRepository()
            {
                Handle = "some-repository-handle",
                PullDogSettings = new PullDogSettings()
                {
                    User = new TestUserBuilder().Build(),
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogPullRequests.AddAsync(new TestPullDogPullRequestBuilder()
                    .WithHandle("some-pull-request-handle")
                    .WithPullDogRepository(pullDogRepository)
                    .Build());
            });

            //Act
            var pullRequest = await environment.Mediator.Send(new EnsurePullDogPullRequestCommand(
                pullDogRepository,
                "some-pull-request-handle"));

            //Assert
            Assert.IsNotNull(pullRequest);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogPullRequests.CountAsync());
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingPullRequestFound_ReturnsNewPullRequest()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var pullDogRepository = new PullDogRepository()
            {
                Handle = "some-repository-handle",
                PullDogSettings = new PullDogSettings()
                {
                    User = new TestUserBuilder().Build(),
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                }
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(0, await dataContext.PullDogPullRequests.CountAsync());
            });

            //Act
            var pullRequest = await environment.Mediator.Send(new EnsurePullDogPullRequestCommand(
                pullDogRepository,
                "some-pull-request-handle"));

            //Assert
            Assert.IsNotNull(pullRequest);

            await environment.WithFreshDataContext(async dataContext =>
            {
                Assert.AreEqual(1, await dataContext.PullDogPullRequests.CountAsync());
            });
        }
    }
}
