using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Commands.Amazon.Lightsail.AssignStaticIpToInstance;
using Dogger.Domain.Commands.Amazon.Lightsail.AttachInstancesToLoadBalancer;
using Dogger.Domain.Commands.Instances.DeleteInstanceByName;
using Dogger.Domain.Queries.Amazon.Lightsail.GetAllInstances;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLoadBalancerByName;
using Dogger.Domain.Queries.Plans.GetSupportedPlans;
using Dogger.Domain.Services.Provisioning;
using Dogger.Infrastructure.Time;
using Dogger.Setup.Domain.Services;
using Dogger.Setup.Tests.TestHelpers.Environments;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Setup.Tests.Domain.Services
{
    [TestClass]
    public class DogfeedServiceTest
    {

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_AllDetailsGiven_AssignsStaticIpAddressToNewInstance()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetAllInstancesQuery>())
                .Returns(
                    new Instance[]
                    {
                        new Instance() {
                            Name = "new-instance"
                        }
                    });

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            await dogfeedService.DogfeedAsync();

            //Assert
            await fakeMediator
                .Received()
                .Send(Arg.Is<AssignStaticIpToInstanceCommand>(
                    arg => arg.InstanceName == "new-instance" && arg.StaticIpName == "main-ip"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_AllDetailsGiven_DestroysOldInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetAllInstancesQuery>())
                .Returns(
                    new Instance[]
                    {
                        new Instance() {
                            Name = "new-instance"
                        },
                        new Instance() {
                            Name = "old-instance"
                        }
                    });

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "old-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            await dogfeedService.DogfeedAsync();

            //Assert
            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "old-instance"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_ExistingDetachedInstances_DestroysDetachedInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetAllInstancesQuery>())
                .Returns(
                    new Instance[]
                    {
                        new Instance() {
                            Name = "some-random-instance"
                        },
                        new Instance() {
                            Name = "main-attached-instance-from-load-balancer"
                        },
                        new Instance() {
                            Name = "main-detached-instance-from-load-balancer"
                        }
                    });

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "main-attached-instance-from-load-balancer",
                                InstanceHealth = InstanceHealthState.Healthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            await dogfeedService.DogfeedAsync();

            //Assert
            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "main-detached-instance-from-load-balancer"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_ExceptionThrownWhileAttachingNewInstanceToLoadBalancer_CleansUpNewInstance()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            fakeMediator
                .Send(Arg.Any<AttachInstancesToLoadBalancerCommand>())
                .Throws(new TestException());

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<TestException>(async () =>
                await dogfeedService.DogfeedAsync());

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "new-instance"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_NewInstanceHealthTimeoutExceeded_ThrowsException()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            var fakeStopwatchWithHighElapsedTime = Substitute.For<IStopwatch>();
            fakeStopwatchWithHighElapsedTime
                .Elapsed
                .Returns(TimeSpan.FromMinutes(31));

            var fakeTime = Substitute.For<ITime>();
            fakeTime
                .StartStopwatch()
                .Returns(fakeStopwatchWithHighElapsedTime);

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                    services.AddSingleton(fakeTime);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<NewInstanceHealthTimeoutException>(async () =>
                await dogfeedService.DogfeedAsync());

            //Assert
            Assert.IsNotNull(exception);

            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "new-instance"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_ExistingRedundantInstances_DestroysRedundantInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "healthy-instance-1",
                                InstanceHealth = InstanceHealthState.Healthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "healthy-instance-2",
                                InstanceHealth = InstanceHealthState.Healthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            await dogfeedService.DogfeedAsync();

            //Assert
            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "healthy-instance-1"));

            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "healthy-instance-2"));
        }

        [TestMethod]
        [TestCategory(TestCategories.IntegrationCategory)]
        public async Task Dogfeed_ExistingUnhealthyInstances_DestroysUnhealthyInstances()
        {
            //Arrange
            var fakeMediator = Substitute.For<IMediator>();
            FakeOutValidSupportedPlans(fakeMediator);
            FakeOutNewlyCreatedInstance(fakeMediator);

            fakeMediator
                .Send(Arg.Any<GetLoadBalancerByNameQuery>())
                .Returns(
                    new LoadBalancer()
                    {
                        InstanceHealthSummary = new List<InstanceHealthSummary>()
                        {
                            new InstanceHealthSummary()
                            {
                                InstanceName = "unhealthy-instance-1",
                                InstanceHealth = InstanceHealthState.Unhealthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "unhealthy-instance-2",
                                InstanceHealth = InstanceHealthState.Unhealthy
                            },
                            new InstanceHealthSummary()
                            {
                                InstanceName = "new-instance",
                                InstanceHealth = InstanceHealthState.Healthy
                            }
                        }
                    });

            await using var environment = await DoggerSetupIntegrationTestEnvironment.CreateAsync(new DoggerSetupEnvironmentSetupOptions()
            {
                IocConfiguration = services =>
                {
                    FakeOutMinimalServices(services);

                    services.AddSingleton(fakeMediator);
                }
            });

            var dogfeedService = environment.ServiceProvider.GetRequiredService<IDogfeedService>();

            //Act
            await dogfeedService.DogfeedAsync();

            //Assert
            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "unhealthy-instance-1"));

            await fakeMediator
                .Received()
                .Send(Arg.Is<DeleteInstanceByNameCommand>(
                    arg => arg.Name == "unhealthy-instance-2"));
        }

        private static void FakeOutNewlyCreatedInstance(IMediator fakeMediator)
        {
            fakeMediator
                .Send(Arg.Any<GetLightsailInstanceByNameQuery>())
                .Returns(new Instance()
                {
                    Name = "new-instance"
                });
        }

        private static void FakeOutMinimalServices(IServiceCollection services)
        {
            var fakeProvisioningService = Substitute.For<IProvisioningService>();
            services.AddSingleton(fakeProvisioningService);
        }

        private static void FakeOutValidSupportedPlans(IMediator fakeMediator)
        {
            fakeMediator
                .Send(Arg.Any<GetSupportedPlansQuery>())
                .Returns(new[]
                {
                    new Dogger.Domain.Queries.Plans.GetSupportedPlans.Plan(
                        "some-bundle-id",
                        1337,
                        new Bundle()
                        {
                            RamSizeInGb = 2,
                            BundleId = "some-bundle-id"
                        },
                        Array.Empty<PullDogPlan>())
                });
        }
    }
}
