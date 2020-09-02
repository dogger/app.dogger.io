using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.OverrideConfigurationForPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class OverrideConfigurationForPullRequestCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullRequestGiven_ConfigurationOverrideUpdatedOnPullRequest()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var pullRequest = new TestPullDogPullRequestBuilder()
                .WithHandle("some-handle")
                .WithPullDogRepository(new TestPullDogRepositoryBuilder().Build())
                .Build();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.PullDogPullRequests.AddAsync(pullRequest);
            });

            //Act
            await environment.Mediator.Send(new OverrideConfigurationForPullRequestCommand(
                pullRequest.Id,
                new ConfigurationFileOverride()
                {
                    BuildArguments = new Dictionary<string, string>()
                    {
                        {
                            "foo", "bar"
                        }
                    }
                }));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedPullRequest = await dataContext
                    .PullDogPullRequests
                    .SingleAsync();
                Assert.IsNotNull(refreshedPullRequest);

                Assert.AreEqual("some-handle", refreshedPullRequest.Handle);
                Assert.AreEqual("bar", refreshedPullRequest.ConfigurationOverride?.BuildArguments["foo"]);
            });
        }
    }
}
