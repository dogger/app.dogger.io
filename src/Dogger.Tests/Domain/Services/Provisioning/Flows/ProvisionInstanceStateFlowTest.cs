using System;
using System.Threading.Tasks;
using Amazon.Lightsail.Model;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Domain.Services.Provisioning.Stages.CompleteInstanceSetup;
using Dogger.Domain.Services.Provisioning.Stages.CreateLightsailInstance;
using Dogger.Domain.Services.Provisioning.Stages.InstallSoftwareOnInstance;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Provisioning.Flows
{
    [TestClass]
    public class ProvisionInstanceStateFlowTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_ValuesGiven_TransfersValuesToInitialState()
        {
            //Arrange
            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = await flow.GetInitialStateAsync(new InitialStateContext(
                fakeMediator,
                stateFactory)) as ICreateLightsailInstanceStage;

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

            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                })
            {
                UserId = fakeUserId
            };

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeState = Substitute.For<ICreateLightsailInstanceStage>();
            fakeState.CreatedLightsailInstance.Returns(new Instance()
            {
                PublicIpAddress = "127.0.0.1"
            });

            //Act
            var state = await flow.GetNextStateAsync(new NextStageContext(
                fakeMediator,
                stateFactory,
                fakeState)) as IInstallSoftwareOnInstanceStage;

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

            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                })
            {
                UserId = fakeUserId
            };

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            var fakeState = Substitute.For<IInstallSoftwareOnInstanceStage>();
            fakeState.IpAddress.Returns("127.0.0.1");

            //Act
            var state = await flow.GetNextStateAsync(new NextStageContext(
                fakeMediator,
                stateFactory,
                fakeState)) as ICompleteInstanceSetupStage;

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
            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeState = Substitute.For<ICompleteInstanceSetupStage>();
            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = await flow.GetNextStateAsync(new NextStageContext(
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
            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStateFactory(serviceProvider);

            var fakeState = Substitute.For<IProvisioningStage>();
            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<UnknownFlowStageException>(async () => 
                await flow.GetNextStateAsync(new NextStageContext(
                    fakeMediator,
                    stateFactory,
                    fakeState)));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}
