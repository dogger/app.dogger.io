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
        public async Task Handle_ExistingClusterInstanceFound_ReusesExistingInstanceAndUpdatesExpiry()
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
                    .SingleOrDefaultAsync(x =>
                        x.Name == oldInstance.Name &&
                        x.Id == oldInstance.Id &&
                        x.ExpiresAtUtc > DateTime.UtcNow.AddDays(7));
                Assert.IsNotNull(refreshedOldInstance);
            });
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_NoExistingClusterInstanceFound_ReturnsNewPersistedInstanceWithProperValuesAndExpiry()
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
                Assert.IsTrue(instance.ExpiresAtUtc > DateTime.UtcNow.AddDays(7));
            });
        }
    }
}
