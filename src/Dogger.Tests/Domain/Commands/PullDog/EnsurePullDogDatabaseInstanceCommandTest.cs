using System;
using System.Threading.Tasks;
using Dogger.Domain.Commands.PullDog.EnsurePullDogDatabaseInstance;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.PullDog.GetAvailableClusterFromPullRequest;
using Dogger.Domain.Services.PullDog;
using Dogger.Tests.TestHelpers;
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var user = new User()
            {
                StripeCustomerId = "dummy"
            };

            var pullDogPullRequest = new PullDogPullRequest()
            {
                Handle = "dummy",
                PullDogRepository = new PullDogRepository()
                {
                    Handle = "dummy",
                    PullDogSettings = new PullDogSettings()
                    {
                        User = user,
                        PoolSize = 1,
                        PlanId = "dummy",
                        EncryptedApiKey = Array.Empty<byte>()
                    }
                }
            };

            var oldInstance = new Instance()
            {
                Name = "existing-instance",
                PlanId = "dummy",
                PullDogPullRequest = pullDogPullRequest,
                Cluster = new Cluster()
                {
                    Name = "pull-dog",
                    User = user
                }
            };

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile()
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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var pullDogPullRequest = new PullDogPullRequest()
            {
                Handle = "dummy",
                PullDogRepository = new PullDogRepository()
                {
                    Handle = "dummy",
                    PullDogSettings = new PullDogSettings()
                    {
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        },
                        PoolSize = 0,
                        PlanId = "dummy",
                        EncryptedApiKey = Array.Empty<byte>()
                    }
                }
            };

            var oldInstance = new Instance()
            {
                Name = "existing-instance",
                PlanId = "dummy",
                PullDogPullRequest = pullDogPullRequest,
                Cluster = new Cluster()
                {
                    Id = DataContext.PullDogDemoClusterId
                }
            };

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile()));

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
            await using var environment = await IntegrationTestEnvironment.CreateAsync();

            var pullDogPullRequest = new PullDogPullRequest()
            {
                Handle = "dummy",
                PullDogRepository = new PullDogRepository()
                {
                    Handle = "dummy",
                    PullDogSettings = new PullDogSettings()
                    {
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        },
                        PoolSize = 0,
                        PlanId = "dummy",
                        EncryptedApiKey = Array.Empty<byte>()
                    }
                }
            };

            var oldInstance = new Instance()
            {
                Name = "existing-instance",
                PlanId = "dummy",
                PullDogPullRequest = pullDogPullRequest,
                Cluster = new Cluster()
                {
                    Id = DataContext.PullDogDemoClusterId
                }
            };

            await environment.DataContext.Instances.AddAsync(oldInstance);
            await environment.DataContext.SaveChangesAsync();

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile()
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
            var pullDogPullRequest = new PullDogPullRequest()
            {
                Handle = "dummy",
                PullDogRepository = new PullDogRepository()
                {
                    Handle = "dummy",
                    PullDogSettings = new PullDogSettings()
                    {
                        User = new User()
                        {
                            StripeCustomerId = "dummy"
                        },
                        PoolSize = 0,
                        PlanId = "dummy",
                        EncryptedApiKey = Array.Empty<byte>()
                    }
                }
            };

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAvailableClusterFromPullRequestQuery>(args =>
                    args.PullRequest == pullDogPullRequest))
                .Returns(new Cluster());

            await using var environment = await IntegrationTestEnvironment.CreateAsync(new EnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile()
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
            var user = new User()
            {
                StripeCustomerId = "dummy"
            };
            var pullDogPullRequest = new PullDogPullRequest()
            {
                Handle = "dummy",
                PullDogRepository = new PullDogRepository()
                {
                    Handle = "dummy",
                    PullDogSettings = new PullDogSettings()
                    {
                        User = user,
                        PoolSize = 1,
                        PlanId = "dummy",
                        EncryptedApiKey = Array.Empty<byte>()
                    }
                }
            };

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetAvailableClusterFromPullRequestQuery>(args =>
                    args.PullRequest == pullDogPullRequest))
                .Returns(new Cluster()
                {
                    Name = "pull-dog",
                    User = user
                });

            await using var environment = await IntegrationTestEnvironment.CreateAsync(new EnvironmentSetupOptions()
            {
                IocConfiguration = services => services.AddSingleton(fakeMediator)
            });

            //Act
            var instance = await environment.Mediator.Send(new EnsurePullDogDatabaseInstanceCommand(
                pullDogPullRequest,
                new ConfigurationFile()
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
