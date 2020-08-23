using System;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.States;
using Dogger.Domain.Services.Provisioning.States.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.States.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.States.InstallSoftwareOnInstance;
using Dogger.Tests.Domain.Models;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Provisioning.Flows
{
    [TestClass]
    public class ProvisionInstanceStateFlowTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_ValuesGiven_TransfersValuesToInitialState()
        {
            //Arrange
            var flow = new ProvisionInstanceStateFlow(
                "some-plan-id",
                new TestInstanceBuilder()
                    .WithName("some-instance-name")
                    .Build());

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = await flow.GetInitialStateAsync(new InitialStateContext(
                fakeMediator,
                stateFactory)) as ICreateLightsailInstanceState;

            //Assert
            Assert.IsNotNull(state);

            Assert.AreEqual("some-plan-id", state.PlanId);
            Assert.AreEqual("some-instance-name", state.DatabaseInstance.Name);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromCreateLightsailInstanceState_ReturnsInstallDockerOnInstanceStateWithValuesTransferred()
        {
            //Arrange
            var fakeUserId = Guid.NewGuid();

            var flow = new ProvisionInstanceStateFlow(
                "some-plan-id",
                new TestInstanceBuilder()
                    .WithName("some-instance-name")
                    .Build());

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeState = Substitute.For<ICreateLightsailInstanceState>();
            fakeState.CreatedLightsailInstance.Returns(new Instance()
            {
                PublicIpAddress = "127.0.0.1"
            });

            //Act
            var state = await flow.GetNextStateAsync(new NextStateContext(
                fakeMediator,
                stateFactory,
                fakeState)) as IInstallSoftwareOnInstanceState;

            //Assert
            Assert.IsNotNull(state);

            Assert.AreEqual("127.0.0.1", state.IpAddress);
            Assert.AreEqual(fakeUserId, state.UserId);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromInstallDockerOnInstanceState_ReturnsCompleteInstanceSetupStateWithValuesTransferred()
        {
            //Arrange
            var fakeUserId = Guid.NewGuid();

            var flow = new ProvisionInstanceStateFlow(
                "some-plan-id",
                new TestInstanceBuilder()
                    .WithName("some-instance-name")
                    .Build())
            {
                UserId = fakeUserId
            };

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeState = Substitute.For<IInstallSoftwareOnInstanceState>();
            fakeState.IpAddress.Returns("127.0.0.1");

            //Act
            var state = await flow.GetNextStateAsync(new NextStateContext(
                fakeMediator,
                stateFactory,
                fakeState)) as ICompleteInstanceSetupState;

            //Assert
            Assert.IsNotNull(state);

            Assert.AreEqual(fakeUserId, state.UserId);

            Assert.AreEqual("127.0.0.1", state.IpAddress);
            Assert.AreEqual("some-instance-name", state.InstanceName);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromCompleteInstanceSetupState_ReturnsNull()
        {
            //Arrange
            var flow = new ProvisionInstanceStateFlow(
                "some-plan-id",
                new TestInstanceBuilder()
                    .WithName("some-instance-name")
                    .Build());

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeState = Substitute.For<ICompleteInstanceSetupState>();
            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = await flow.GetNextStateAsync(new NextStateContext(
                fakeMediator,
                stateFactory,
                fakeState));

            //Assert
            Assert.IsNull(state);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromUnknownState_ThrowsException()
        {
            //Arrange
            var flow = new ProvisionInstanceStateFlow(
                "some-plan-id",
                new TestInstanceBuilder()
                    .WithName("some-instance-name")
                    .Build());

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeState = Substitute.For<IProvisioningState>();
            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<UnknownFlowStateException>(async () =>
                await flow.GetNextStateAsync(new NextStateContext(
                    fakeMediator,
                    stateFactory,
                    fakeState)));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}
