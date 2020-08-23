using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lightsail;
using Amazon.Lightsail.Model;
using Dogger.Domain.Models;
using Dogger.Domain.Queries.Amazon.Lightsail.GetLightsailInstanceByName;
using Dogger.Domain.Services.Amazon.Lightsail;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance;
using Dogger.Infrastructure.Ssh;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Instance = Amazon.Lightsail.Model.Instance;

namespace Dogger.Tests.Domain.Provisioning.States
{
    [TestClass]
    public class CreateLightsailInstanceStateTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_HasFailedOperation_ThrowsStateUpdateException()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = OperationStatus.Failed
                    }
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "dummy",
                Cluster = new TestClusterBuilder().Build()
            };

            await state.InitializeAsync();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<StateUpdateException>(async () =>
                await state.UpdateAsync());

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_HasUnknownStatusOperation_ReturnsItself()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = new OperationStatus("some-invalid-value")
                    }
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "dummy"
            };

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () =>
                await state.UpdateAsync());

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_HasNotStartedOperation_ReturnsInProgress()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = OperationStatus.NotStarted
                    }
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "dummy",
                Cluster = new TestClusterBuilder().Build()
            };

            await state.InitializeAsync();

            //Act
            var result = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.InProgress, result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_HasStartedOperation_ReturnsInProgress()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = OperationStatus.Started
                    }
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "dummy",
                Cluster = new TestClusterBuilder().Build()
            };

            await state.InitializeAsync();

            //Act
            var result = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.InProgress, result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_AllOperationsSucceededAndInstanceRunning_ReturnsSucceeded()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = OperationStatus.Succeeded
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                    arg.Name == "some-instance-name"))
                .Returns(new Instance()
                {
                    State = new InstanceState()
                    {
                        Name = "running"
                    }
                });

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<InstallSoftwareOnInstanceState>()
                .Returns(new InstallSoftwareOnInstanceState(
                    Substitute.For<ISshClientFactory>(),
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "some-instance-name",
                Cluster = new TestClusterBuilder().Build()
            };

            await state.InitializeAsync();

            //Act
            var result = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.Succeeded, result);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Initialize_DatabaseInstanceWithClusterSet_CreatesNecessaryTags()
        {
            //Arrange
            var fakeUserId = Guid.NewGuid();
            var fakeClusterId = Guid.NewGuid();
            var fakeInstanceId = Guid.NewGuid();

            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "some-plan-id";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Id = fakeInstanceId,
                Name = "some-instance-name",
                Cluster = new TestClusterBuilder()
                    .WithId(fakeClusterId)
                    .WithUser(fakeUserId)
                    .Build()
            };

            //Act
            await state.InitializeAsync();

            //Assert
            await fakeAmazonLightsail
                .Received(1)
                .CreateInstancesAsync(Arg.Is<CreateInstancesRequest>(args =>
                    args.Tags.Any(x => x.Key == "UserId" && x.Value == fakeUserId.ToString()) &&
                    args.Tags.Any(x => x.Key == "StripePlanId" && x.Value == "some-plan-id") &&
                    args.Tags.Any(x => x.Key == "ClusterId" && x.Value == fakeClusterId.ToString()) &&
                    args.Tags.Any(x => x.Key == "InstanceId" && x.Value == fakeInstanceId.ToString())));
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task Update_AllOperationsSucceededAndInstanceNotRunning_ReturnsInProgress()
        {
            //Arrange
            var fakeAmazonLightsail = Substitute.For<IAmazonLightsail>();
            fakeAmazonLightsail
                .CreateInstancesAsync(Arg.Any<CreateInstancesRequest>())
                .Returns(new CreateInstancesResponse()
                {
                    Operations = new List<Operation>()
                });

            var fakeLightsailOperationService = Substitute.For<ILightsailOperationService>();
            fakeLightsailOperationService
                .GetOperationsFromIdsAsync(Arg.Any<IEnumerable<string>>())
                .Returns(new[]
                {
                    new Operation()
                    {
                        Status = OperationStatus.Succeeded
                    }
                });

            var fakeMediator = Substitute.For<IMediator>();
            fakeMediator
                .Send(Arg.Is<GetLightsailInstanceByNameQuery>(arg =>
                    arg.Name == "some-instance-name"))
                .Returns(new Instance()
                {
                    State = new InstanceState()
                    {
                        Name = "not-running"
                    }
                });

            var fakeProvisioningStateFactory = Substitute.For<IProvisioningStateFactory>();
            fakeProvisioningStateFactory
                .Create<InstallSoftwareOnInstanceState>()
                .Returns(new InstallSoftwareOnInstanceState(
                    Substitute.For<ISshClientFactory>(),
                    Substitute.For<IMediator>()));

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup(services =>
            {
                services.AddSingleton(fakeAmazonLightsail);
                services.AddSingleton(fakeLightsailOperationService);
                services.AddSingleton(fakeMediator);
            });

            var state = serviceProvider.GetRequiredService<CreateLightsailInstanceState>();
            state.PlanId = "dummy";
            state.DatabaseInstance = new Dogger.Domain.Models.Instance()
            {
                Name = "some-instance-name",
                Cluster = new TestClusterBuilder().Build()
            };

            await state.InitializeAsync();

            //Act
            var newState = await state.UpdateAsync();

            //Assert
            Assert.AreEqual(ProvisioningStateUpdateResult.InProgress, newState);
        }
    }
}
