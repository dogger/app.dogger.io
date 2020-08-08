using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Models;
using Dogger.Domain.Services.Provisioning;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Infrastructure.IO;
using Dogger.Setup.Domain.Commands.ProvisionDogfeedInstance;
using Dogger.Setup.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers;
using Dogger.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers.Environments.Dogger;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Setup.Tests.Domain.Commands
{
    [TestClass]
    public class ProvisionDogfeedInstanceCommandTest
    {
        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_JobIsQueuedWithProperPlanId()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeFile = Substitute.For<IFile>();

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                    services.AddSingleton(fakeFile);
                }
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDogfeedInstanceCommand(
                    "some-instance-name"));

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<AggregateProvisioningStateFlow>(arguments => GetProvisionInstanceStateFlow(arguments)
                        .PlanId == "some-plan-id"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_JobIsQueuedWithProperInstanceName()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeFile = Substitute.For<IFile>();

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                    services.AddSingleton(fakeFile);
                }
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDogfeedInstanceCommand(
                    "some-instance-name"));

            //Assert
            await fakeProvisioningService
                .Received(1)
                .ScheduleJobAsync(
                    Arg.Is<AggregateProvisioningStateFlow>(arguments => GetProvisionInstanceStateFlow(arguments)
                        .DatabaseInstance.Name == "some-instance-name"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Handle_ProperArgumentsGiven_DoggerInstanceIsCreatedInDatabase()
        {
            //Arrange
            var fakeProvisioningService = Substitute.For<IProvisioningService>();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            FakeOutBundleFetching(fakeAmazonLightsail);

            var fakeFile = Substitute.For<IFile>();

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    services.AddSingleton(fakeProvisioningService);
                    services.AddSingleton(fakeAmazonLightsail);
                    services.AddSingleton(fakeFile);
                }
            });

            //Act
            await environment.Mediator.Send(
                new ProvisionDogfeedInstanceCommand(
                    "some-instance-name"));

            //Assert
            await environment.WithFreshDataContext(async dataContext =>
            {
                var cluster = await dataContext
                    .Clusters
                    .Include(x => x.Instances)
                    .SingleOrDefaultAsync();
                Assert.IsNotNull(cluster);

                Assert.AreEqual(DataContext.DoggerClusterId, cluster.Id);
                Assert.AreEqual(1, cluster.Instances.Count);
                Assert.AreEqual("some-instance-name", cluster.Instances.Single().Name);
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

        private static ProvisionInstanceStateFlow GetProvisionInstanceStateFlow(
            AggregateProvisioningStateFlow flow)
        {
            return (ProvisionInstanceStateFlow)flow.Flows[0];
        }
    }
}
