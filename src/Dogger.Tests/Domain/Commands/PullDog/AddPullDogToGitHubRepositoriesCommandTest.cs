using System;
using System.Linq;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.AddPullDogToGitHubRepositories;
using Dogger.Domain.Models;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class AddPullDogToGitHubRepositoriesCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_MultipleRepositoryIdsSpecified_RepositoriesAdded()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync();

            var user = new TestUserBuilder()
                .WithPullDogSettings()
                .Build();
            await environment.DataContext.Users.AddAsync(user);
            await environment.DataContext.SaveChangesAsync();

            //Act
            await environment.Mediator.Send(new AddPullDogToGitHubRepositoriesCommand(
                1337,
                user.PullDogSettings,
                new[]
                {
                    1338L,
                    1339L
                }));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var repositories = await dataContext
                    .PullDogRepositories
                    .AsQueryable()
                    .ToListAsync();
                Assert.AreEqual(2, repositories.Count);

                Assert.IsTrue(repositories.Any(repository =>
                    repository.GitHubInstallationId == 1337 &&
                    repository.PullDogSettingsId == user.PullDogSettings.Id &&
                    repository.Handle == "1338"));

                Assert.IsTrue(repositories.Any(repository =>
                    repository.GitHubInstallationId == 1337 &&
                    repository.PullDogSettingsId == user.PullDogSettings.Id &&
                    repository.Handle == "1339"));
            });
        }
    }
}
