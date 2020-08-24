using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromGitHub;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromGitHubUserId;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Infrastructure.GitHub;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Octokit;
using User = Octokit.User;

namespace Dogger.Tests.Domain.Commands.PullDog.InstallPullDogFromGitHub
{
    [TestClass]
    public class InstallPullDogFromGitHubCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoValidatedEmailsFound_ThrowsException()
        {
            //Arrange
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationInitiatorClientAsync("some-code");

            fakeGitHubClient
                .User
                .Current()
                .Returns(new User());

            fakeGitHubClient
                .User
                .Email
                .GetAll()
                .Returns(Array.Empty<EmailAddress>());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeGitHubClientFactory)
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(new InstallPullDogFromGitHubCommand(
                    "some-code",
                    1337)));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingAuth0UserByGitHubUserIdPresent_DoesNothing()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClientFactory);
                    services.AddSingleton(fakeMediator);
                }
            });

            var userInDatabase = new TestUserBuilder()
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPlanId("some-plan-id")
                    .WithRepositories(
                        new TestPullDogRepositoryBuilder()
                            .WithGitHubInstallationId(1337)
                            .Build())
                    .Build())
                .Build();
            await environment.DataContext.Users.AddAsync(userInDatabase);
            await environment.DataContext.SaveChangesAsync();

            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(
                    new Dogger.Domain.Queries.Plans.GetSupportedPlans.Plan(
                        "demo-plan-id",
                        1337,
                        new Bundle()
                        {
                            Price = 1337,
                            BundleId = "demo-plan-id"
                        },
                        Array.Empty<PullDogPlan>()));

            fakeMediator
                .Send(Arg.Any<GetAuth0UserFromGitHubUserIdQuery>())
                .Returns(new global::Auth0.ManagementApi.Models.User()
                {
                    UserId = "auth0-user-id"
                });

            fakeMediator
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.First() == "email-2@example.com"))
                .Returns(userInDatabase);

            var fakeGitHubInstallationClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubInstallationClient
                .GitHubApps
                .Installation
                .GetAllRepositoriesForCurrent()
                .Returns(new RepositoriesResponse(0, Array.Empty<Repository>()));

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationInitiatorClientAsync("some-code");
            fakeGitHubClient
                .User
                .Current()
                .Returns(new User());

            fakeGitHubClient
                .User
                .Email
                .GetAll()
                .Returns(new[]
                {
                    new EmailAddress("email-1@example.com", true, false, EmailVisibility.Public),
                    new EmailAddress("email-2@example.com", true, true, EmailVisibility.Public),
                    new EmailAddress("email-3@example.com", true, false, EmailVisibility.Public)
                });

            //Act
            await environment.Mediator.Send(new InstallPullDogFromGitHubCommand(
                "some-code",
                1337));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var user = await dataContext
                    .Users
                    .Include(x => x.PullDogSettings)
                    .ThenInclude(x => x.Repositories)
                    .SingleAsync();
                var settings = user.PullDogSettings;

                Assert.AreEqual(settings.Repositories.Single().GitHubInstallationId, 1337);
                Assert.AreEqual(settings.PlanId, "some-plan-id");
                Assert.AreEqual(settings.PoolSize, 0);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogSettingsAlreadyExist_UpdatesInstallationId()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            var fakeGitHubClientFactory = Substitute.For<IGitHubClientFactory>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeGitHubClientFactory);
                    services.AddSingleton(fakeMediator);
                }
            });

            var userInDatabase = new TestUserBuilder()
                .WithPullDogSettings(new TestPullDogSettingsBuilder()
                    .WithPlanId("some-plan-id")
                    .WithRepositories(new TestPullDogRepositoryBuilder()
                        .WithGitHubInstallationId(1337)
                        .Build())
                    .Build())
                .Build();
            await environment.DataContext.Users.AddAsync(userInDatabase);
            await environment.DataContext.SaveChangesAsync();

            fakeMediator
                .Send(Arg.Any<GetAuth0UserFromEmailsQuery>())
                .Returns(new global::Auth0.ManagementApi.Models.User()
                {
                    UserId = "auth0-user-id"
                });

            fakeMediator
                .Send(Arg.Is<InstallPullDogFromEmailsCommand>(args =>
                    args.Emails.First() == "email-2@example.com"))
                .Returns(userInDatabase);

            var fakeGitHubInstallationClient = await fakeGitHubClientFactory.CreateInstallationClientAsync(1337);
            fakeGitHubInstallationClient
                .GitHubApps
                .Installation
                .GetAllRepositoriesForCurrent()
                .Returns(new RepositoriesResponse(0, Array.Empty<Repository>()));

            var fakeGitHubClient = await fakeGitHubClientFactory.CreateInstallationInitiatorClientAsync("some-code");
            fakeGitHubClient
                .User
                .Current()
                .Returns(new User());

            fakeGitHubClient
                .GitHubApps
                .Installation
                .GetAllRepositoriesForCurrent()
                .Returns(new RepositoriesResponse(0, Array.Empty<Repository>()));

            fakeGitHubClient
                .User
                .Email
                .GetAll()
                .Returns(new[]
                {
                    new EmailAddress("email-1@example.com", true, false, EmailVisibility.Public),
                    new EmailAddress("email-2@example.com", true, true, EmailVisibility.Public),
                    new EmailAddress("email-3@example.com", true, false, EmailVisibility.Public)
                });

            //Act
            await environment.Mediator.Send(new InstallPullDogFromGitHubCommand(
                "some-code",
                1337));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var user = await dataContext
                    .Users
                    .Include(x => x.PullDogSettings)
                    .ThenInclude(x => x.Repositories)
                    .SingleAsync();
                var settings = user.PullDogSettings;
                Assert.IsNotNull(settings);
                Assert.AreEqual("some-plan-id", settings.PlanId);
                Assert.AreEqual(1337, settings.Repositories.Single().GitHubInstallationId);
            });
        }
    }
}
