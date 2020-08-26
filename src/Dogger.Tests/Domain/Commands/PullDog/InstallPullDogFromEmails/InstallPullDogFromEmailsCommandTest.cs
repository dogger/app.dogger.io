using System;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Auth0.CreateAuth0User;
using Dogger.Domain.Commands.PullDog.InstallPullDogFromEmails;
using Dogger.Domain.Commands.Users.EnsureUserForIdentity;
using Dogger.Domain.Queries.Auth0.GetAuth0UserFromEmails;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Dogger.Tests.Domain.Models;

namespace Dogger.Tests.Domain.Commands.PullDog.InstallPullDogFromEmails
{
    [TestClass]
    public class InstallPullDogFromEmailsCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingAuth0UserByEmailsPresent_SetsSettingsOnUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeMediator);
                }
            });

            var userInDatabase = new TestUserBuilder().Build();
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
                .Send(Arg.Any<GetAuth0UserFromEmailsQuery>())
                .Returns(new global::Auth0.ManagementApi.Models.User()
                {
                    UserId = "auth0-user-id"
                });

            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(args =>
                    args.Email == "email-1@example.com" &&
                    args.IdentityName == "auth0-user-id"))
                .Returns(userInDatabase);

            //Act
            await environment.Mediator.Send(new InstallPullDogFromEmailsCommand(
                new[]
                {
                    "email-1@example.com",
                    "email-2@example.com"
                }));

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
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingAuth0UserPresent_CreatesNewAuth0User()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeMediator);
                }
            });

            var userInDatabase = new TestUserBuilder().Build();
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
                .Send(Arg.Any<CreateAuth0UserCommand>())
                .Returns(new global::Auth0.ManagementApi.Models.User()
                {
                    UserId = "auth0-user-id"
                });

            fakeMediator
                .Send(Arg.Is<EnsureUserForIdentityCommand>(args =>
                    args.Email == "email-1@example.com" &&
                    args.IdentityName == "auth0-user-id"))
                .Returns(userInDatabase);

            //Act
            await environment.Mediator.Send(new InstallPullDogFromEmailsCommand(
                new[]
                {
                    "email-1@example.com",
                    "email-2@example.com"
                }));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var user = await dataContext
                    .Users
                    .Include(x => x.PullDogSettings)
                    .SingleAsync();
                var settings = user.PullDogSettings;
                Assert.IsNotNull(settings);
            });

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<CreateAuth0UserCommand>());
        }
    }
}

