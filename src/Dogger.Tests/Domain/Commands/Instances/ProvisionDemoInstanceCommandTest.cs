using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Instances.ProvisionDemoInstance;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Instance = Dogger.Domain.Models.Instance;

namespace Dogger.Tests.Domain.Commands.Instances
{
    [TestClass]
    public class ProvisionDemoInstanceCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingDemoInstanceInClusterByGuestAndNotAuthenticated_ExceptionThrown()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                }
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                var demoCluster = new TestClusterBuilder()
                    .WithUser()
                    .WithId(DataContext.DemoClusterId)
                    .WithInstances(new TestInstanceBuilder().Build())
                    .Build();
                await dataContext.Clusters.AddAsync(demoCluster);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<DemoInstanceAlreadyProvisionedException>(async () =>
                await environment.Mediator.Send(
                    new ProvisionDemoInstanceCommand()
                    {
                        AuthenticatedUserId = null
                    }));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingDemoInstanceInClusterByUserAndNotAuthenticated_ExceptionThrown()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                }
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                var demoCluster = new TestClusterBuilder()
                    .WithUser()
                    .WithId(DataContext.DemoClusterId)
                    .WithInstances(new TestInstanceBuilder().Build())
                    .Build();
                await dataContext.Clusters.AddAsync(demoCluster);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<DemoInstanceAlreadyProvisionedException>(async () =>
                await environment.Mediator.Send(
                    new ProvisionDemoInstanceCommand()
                    {
                        AuthenticatedUserId = null
                    }));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingDemoInstanceInClusterByUserAndAuthenticatedAsDifferentUser_ExceptionThrown()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                }
            });

            await environment.WithFreshDataContext(async dataContext =>
            {
                var demoCluster = new TestClusterBuilder()
                    .WithUser()
                    .WithId(DataContext.DemoClusterId)
                    .WithInstances(new TestInstanceBuilder().Build())
                    .Build();
                await dataContext.Clusters.AddAsync(demoCluster);
            });

            //Act
            var exception = await Assert.ThrowsExceptionAsync<DemoInstanceAlreadyProvisionedException>(async () =>
                await environment.Mediator.Send(
                    new ProvisionDemoInstanceCommand()
                    {
                        AuthenticatedUserId = Guid.NewGuid()
                    }));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ExistingDemoInstanceInClusterByUserAndAuthenticatedAsSameUser_ReturnsCompletedJob()
        {
            //Arrange
            var fakeCompletedJob = Substitute.For<IProvisioningJob>();

            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            fakeProvisioningService
                .GetCompletedJob()
                .Returns(fakeCompletedJob);

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                }
            });

            var userId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                var demoCluster = new TestClusterBuilder()
                    .WithUser()
                    .WithId(DataContext.DemoClusterId)
                    .WithInstances(new TestInstanceBuilder().Build())
                    .Build();
                await dataContext.Clusters.AddAsync(demoCluster);
            });

            //Act
            var job = await environment.Mediator.Send(new ProvisionDemoInstanceCommand()
            {
                AuthenticatedUserId = userId
            });

            //Assert
            Assert.IsNotNull(job);
            Assert.AreSame(fakeCompletedJob, job);
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_JobIsQueuedWithProperPlanId()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                }
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDemoInstanceCommand());

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<ProvisionInstanceStateFlow>(arguments =>
                        arguments.PlanId == "some-plan-id"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_JobIsQueuedWithProperInstanceName()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                }
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDemoInstanceCommand());

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<ProvisionInstanceStateFlow>(arguments =>
                        arguments.DatabaseInstance.Name == "demo"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGivenWithAuthenticatedUser_DemoInstanceIsCreatedInDatabase()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            await using var environment = await DoggerIntegrationTestEnvironment.CreateAsync(new DoggerEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                }
            });

            var someAuthenticatedUserId = Guid.NewGuid();

            await environment.WithFreshDataContext(async dataContext =>
            {
                await dataContext.Users.AddAsync(new TestUserBuilder()
                    .WithId(someAuthenticatedUserId)
                    .Build());
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDemoInstanceCommand()
                {
                    AuthenticatedUserId = someAuthenticatedUserId
                });

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var cluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .SingleOrDefaultAsync();
                Assert.IsNotNull(cluster);

                Assert.AreEqual(cluster.UserId, someAuthenticatedUserId);
                Assert.AreEqual(DataContext.DemoClusterId, cluster.Id);

                Assert.AreEqual(1, cluster.Instances.Count);
                Assert.AreEqual("demo", cluster.Instances.Single().Name);
            });
        }

        private static void FakeOutBundleFetching(IAmazonLightsail fakeAmazonLightsail)
        {
            fakeAmazonLightsail
                .GetBundlesAsync(Arg.Any<GetBundlesRequest>())
                .Returns(new GetBundlesResponse()
                {
                    Bundles = new List<Bundle>()
                    {
                        new Bundle()
                        {
                            RamSizeInGb = 32,
                            IsActive = true,
                            BundleId = "some-plan-id",
                            SupportedPlatforms = new List<string>()
                            {
                                "LINUX_UNIX"
                            }
                        }
                    }
                });
        }
    }
}
