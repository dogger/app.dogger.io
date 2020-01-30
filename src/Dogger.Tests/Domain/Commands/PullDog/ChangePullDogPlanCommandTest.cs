using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Payment.UpdateUserSubscription;
using Dogger.Domain.Commands.PullDog.ChangePullDogPlan;
using Dogger.Domain.Commands.PullDog.DeleteAllPullDogInstancesForUser;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Plans.GetDemoPlan;
using Dogger.Domain.Queries.Plans.GetPlanById;
using Dogger.Domain.Queries.Plans.GetPullDogPlanFromSettings;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Queries.Users.GetUserById;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class ChangePullDogPlanCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_PullDogNotInstalled_ThrowsException()
        {
            //Arrange
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var user = new User()
            {
                PullDogSettings = null,
                StripeCustomerId = "dummy"
            };
            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(user);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await environment.Mediator.Send(new ChangePullDogPlanCommand(
                    user.Id, 
                    1337,
                    "some-plan-id")));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InvalidDoggerPlanProvided_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args => 
                    args.DoggerPlanId == "some-plan-id"))
                .Returns((PullDogPlan)null);

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new User()
            {
                PullDogSettings = new PullDogSettings()
                {
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                },
                StripeCustomerId = "dummy"
            };
            fakeMediator
                .Send(Arg.Is<GetUserByIdQuery>(args => args.UserId == user.Id))
                .Returns(user);

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(user));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(new ChangePullDogPlanCommand(
                    user.Id,
                    1337,
                    "some-plan-id")));

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_InvalidPullDogPlanPoolSizeProvided_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-new-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1337));

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var user = new User()
            {
                PullDogSettings = new PullDogSettings()
                {
                    PlanId = "dummy",
                    EncryptedApiKey = Array.Empty<byte>()
                },
                StripeCustomerId = "dummy"
            };
            fakeMediator
                .Send(Arg.Is<GetUserByIdQuery>(args => args.UserId == user.Id))
                .Returns(user);

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(user));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await environment.Mediator.Send(new ChangePullDogPlanCommand(
                    user.Id,
                    1338,
                    "some-plan-id")));

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .Received(1)
                .Send(Arg.Any<GetPullDogPlanFromSettingsQuery>());
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditions_PersistsChangesToDatabase()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-new-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1338));

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "dummy"
                }
            };
            fakeMediator
                .Send(Arg.Is<GetUserByIdQuery>(args => args.UserId == databaseUser.Id))
                .Returns(databaseUser);

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(databaseUser));

            //Act
            await environment.Mediator.Send(new ChangePullDogPlanCommand(
                databaseUser.Id,
                1338,
                "some-new-plan-id"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedUser = await dataContext
                    .Users
                    .Include(x => x.PullDogSettings)
                    .SingleOrDefaultAsync();
                var refreshedSettings = refreshedUser.PullDogSettings;
                Assert.IsNotNull(refreshedSettings);

                Assert.AreEqual(1338, refreshedSettings.PoolSize);
                Assert.AreEqual("some-new-plan-id", refreshedSettings.PlanId);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditionsWithDemoPlan_PersistsChangesToDatabase()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Any<GetDemoPlanQuery>())
                .Returns(new Plan(
                    "some-demo-plan-id",
                    0,
                    new Bundle(), 
                    Array.Empty<PullDogPlan>()));

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "dummy"
                }
            };
            fakeMediator
                .Send(Arg.Is<GetUserByIdQuery>(args => args.UserId == databaseUser.Id))
                .Returns(databaseUser);

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(databaseUser);
            });

            //Act
            await environment.Mediator.Send(new ChangePullDogPlanCommand(
                databaseUser.Id,
                0,
                "some-non-existing-plan-id"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedSettings = await dataContext
                    .PullDogSettings
                    .SingleOrDefaultAsync();
                Assert.AreEqual(0, refreshedSettings.PoolSize);
                Assert.AreEqual("some-demo-plan-id", refreshedSettings.PlanId);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditionsWithPaidPlan_UpdatesSubscriptions()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-new-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1338));

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "dummy"
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(databaseUser);
            });

            //Act
            await environment.Mediator.Send(new ChangePullDogPlanCommand(
                databaseUser.Id,
                1338,
                "some-new-plan-id"));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<UpdateUserSubscriptionCommand>(args =>
                    args.UserId == databaseUser.Id));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_SubscriptionsUpdateThrowsException_NothingIsCommitted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-new-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1338));

            fakeMediator
                .Send(Arg.Any<UpdateUserSubscriptionCommand>())
                .Throws(new TestException());

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "some-plan-id"
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(databaseUser);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await environment.Mediator.Send(new ChangePullDogPlanCommand(
                    databaseUser.Id,
                    1338,
                    "some-new-plan-id")));

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedSettings = await dataContext
                    .PullDogSettings
                    .SingleOrDefaultAsync();
                Assert.IsNotNull(refreshedSettings);

                Assert.AreEqual(1337, refreshedSettings.PoolSize);
                Assert.AreEqual("some-plan-id", refreshedSettings.PlanId);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_DeletionOfInstancesThrowsException_NothingIsCommitted()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-new-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1338));

            fakeMediator
                .Send(Arg.Any<DeleteAllPullDogInstancesForUserCommand>())
                .Throws(new TestException());

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "some-plan-id"
                }
            };
            fakeMediator
                .Send(Arg.Is<GetUserByIdQuery>(args => args.UserId == databaseUser.Id))
                .Returns(databaseUser);

            await environment.WithFreshDataContext(async dataContext =>
                await dataContext.Users.AddAsync(databaseUser));

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () => 
                await environment.Mediator.Send(new ChangePullDogPlanCommand(
                    databaseUser.Id,
                    1338,
                    "some-new-plan-id")));

            //Assert
            Assert.IsNotNull(exception);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedSettings = await dataContext
                    .PullDogSettings
                    .AsNoTracking()
                    .SingleOrDefaultAsync();
                Assert.IsNotNull(refreshedSettings);

                Assert.AreEqual(1337, refreshedSettings.PoolSize);
                Assert.AreEqual("some-plan-id", refreshedSettings.PlanId);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ValidConditions_DeletesAllPullDogInstancesForUser()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetPullDogPlanFromSettingsQuery>(args =>
                    args.DoggerPlanId == "some-plan-id"))
                .Returns(new PullDogPlan(
                    "some-plan-id",
                    1337,
                    1337));

            await using var environment = await IntegrationTestEnvironment.CreateAsync(
                new EnvironmentSetupOptions()
                {
                    IocConfiguration = services => services.AddSingleton(fakeMediator)
                });

            var databaseUser = new User()
            {
                StripeCustomerId = "dummy",
                PullDogSettings = new PullDogSettings()
                {
                    PoolSize = 1337,
                    EncryptedApiKey = Array.Empty<byte>(),
                    PlanId = "dummy"
                }
            };

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(databaseUser);
            });

            //Act
            await environment.Mediator.Send(new ChangePullDogPlanCommand(
                databaseUser.Id,
                1337,
                "some-plan-id"));

            //Assert
            await fakeMediator
                .Received(1)
                .Send(Arg.Is<DeleteAllPullDogInstancesForUserCommand>(args =>
                    args.UserId == databaseUser.Id));
        }
    }
}
