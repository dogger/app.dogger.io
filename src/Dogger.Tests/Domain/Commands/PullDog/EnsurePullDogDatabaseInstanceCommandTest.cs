using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Builders.Models;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Commands.PullDog
{
    [TestClass]
    public class EnsurePullDogDatabaseInstanceCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingClusterInstanceFoundAndPaidUser_ReusesExistingInstanceAndUpdatesExpiry()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(Substitute.For<IPullDogRepositoryClientFactory>());
                }
            });

            var user = new TestUserBuilder().Build();

            var pullDogPullRequest = new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithUser(user)
                        .WithPoolSize(1)))
                .Build();

            var oldInstance = new TestInstanceBuilder()
                .WithName("existing-instance")
                .WithPullDogPullRequest(pullDogPullRequest)
                .WithCluster(new TestClusterBuilder()
                    .WithName("pull-dog")
                    .WithUser(user))
                .Build();

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile(new List<string>())
                {
                    Expiry = TimeSpan.FromDays(30)
                }));

            //Assert
            Assert.AreSame(instance, oldInstance);
            Assert.AreEqual(instance.Name, oldInstance.Name);
            Assert.AreEqual(instance.Id, oldInstance.Id);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedOldInstance = await dataContext
                    .Instances
                    .AsNoTracking()
                    .SingleOrDefaultAsync(x =>
                        x.Name == oldInstance.Name &&
                        x.Id == oldInstance.Id &&
                        x.ExpiresAtUtc > DateTime.UtcNow.AddDays(7));
                Assert.IsNotNull(refreshedOldInstance);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingClusterInstanceFoundAndDemoUserWithNoExpiry_ReducesExpiryToAnHour()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(Substitute.For<IPullDogRepositoryClientFactory>());
                }
            });

            var pullDogPullRequest = new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new TestPullDogRepositoryBuilder().Build())
                .Build();

            var oldInstance = new TestInstanceBuilder()
                .WithName("existing-instance")
                .WithPullDogPullRequest(pullDogPullRequest)
                .WithCluster(new TestClusterBuilder()
                    .WithId(DataContext.PullDogDemoClusterId))
                .Build();

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile(new List<string>())));

            //Assert
            Assert.AreSame(instance, oldInstance);
            Assert.AreEqual(instance.Name, oldInstance.Name);
            Assert.AreEqual(instance.Id, oldInstance.Id);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedOldInstance = await dataContext
                    .Instances
                    .AsNoTracking()
                    .SingleAsync();

                Assert.AreEqual(oldInstance.Name, refreshedOldInstance.Name);
                Assert.AreEqual(oldInstance.Id, refreshedOldInstance.Id);
                Assert.IsTrue(refreshedOldInstance.ExpiresAtUtc < DateTime.UtcNow.AddHours(1));
                Assert.IsTrue(refreshedOldInstance.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(50));
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingClusterInstanceFoundAndDemoUserWithMoreThanOneHourExpiry_ReducesExpiryToAnHour()
        {
            //Arrange
            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(Substitute.For<IPullDogRepositoryClientFactory>());
                }
            });

            var pullDogPullRequest = new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new TestPullDogRepositoryBuilder().Build())
                .Build();

            var oldInstance = new TestInstanceBuilder()
                .WithName("existing-instance")
                .WithPullDogPullRequest(pullDogPullRequest)
                .WithCluster(new TestClusterBuilder()
                    .WithId(DataContext.PullDogDemoClusterId))
                .Build();

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile(new List<string>())
                {
                    Expiry = TimeSpan.FromDays(30)
                }));

            //Assert
            Assert.AreSame(instance, oldInstance);
            Assert.AreEqual(instance.Name, oldInstance.Name);
            Assert.AreEqual(instance.Id, oldInstance.Id);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedOldInstance = await dataContext
                    .Instances
                    .AsNoTracking()
                    .SingleAsync();

                Assert.AreEqual(oldInstance.Name, refreshedOldInstance.Name);
                Assert.AreEqual(oldInstance.Id, refreshedOldInstance.Id);
                Assert.IsTrue(refreshedOldInstance.ExpiresAtUtc < DateTime.UtcNow.AddHours(1));
                Assert.IsTrue(refreshedOldInstance.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(50));
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingClusterInstanceFoundAndDemoUser_ReturnsNewPersistedInstanceWithProperValuesAndExpiryOfAnHour()
        {
            //Arrange
            var pullDogPullRequest = new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new TestPullDogRepositoryBuilder().Build())
                .Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAvailableClusterFromPullRequestQuery>(args =>
                    args.PullRequest == pullDogPullRequest))
                .Returns(new TestClusterBuilder().Build());

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile(new List<string>())
                {
                    Expiry = TimeSpan.FromDays(30)
                }));

            //Assert
            Assert.IsNotNull(instance);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedInstance = await dataContext
                    .Instances
                    .SingleAsync();
                Assert.AreEqual(instance.Id, refreshedInstance.Id);
                Assert.IsTrue(
                    instance.ExpiresAtUtc < DateTime.UtcNow.AddHours(1) &&
                    instance.ExpiresAtUtc > DateTime.UtcNow.AddMinutes(50));
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingClusterInstanceFoundAndPaidUser_ReturnsNewPersistedInstanceWithProperValuesAndExpiry()
        {
            //Arrange
            var user = new TestUserBuilder().Build();
            var pullDogPullRequest = new TestPullDogPullRequestBuilder()
                .WithPullDogRepository(new TestPullDogRepositoryBuilder()
                    .WithPullDogSettings(new TestPullDogSettingsBuilder()
                        .WithUser(user)
                        .WithPoolSize(1)))
                .Build();

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAvailableClusterFromPullRequestQuery>(args =>
                    args.PullRequest == pullDogPullRequest))
                .Returns(new TestClusterBuilder()
                    .WithName("pull-dog")
                    .WithUser(user));

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile(new List<string>())
                {
                    Expiry = TimeSpan.FromDays(30)
                }));

            //Assert
            Assert.IsNotNull(instance);

            await environment.WithFreshDataContext(async dataContext =>
            {
                var refreshedInstance = await dataContext
                    .Instances
                    .SingleAsync();
                Assert.AreEqual(instance.Id, refreshedInstance.Id);
                Assert.IsTrue(instance.ExpiresAtUtc > DateTime.UtcNow.AddDays(7));
            });
        }
    }
}
