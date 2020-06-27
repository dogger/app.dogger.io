using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Stages;
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
            var stateFactory = new ProvisioningStageFactory(serviceProvider);

            var fakeMediator = Substitute.For<IMediator>();

            //Act
            var state = flow.GetInitialState(stateFactory) as ICreateLightsailInstanceStage;

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
            var stateFactory = new ProvisioningStageFactory(serviceProvider);

            var fakeState = Substitute.For<ICreateLightsailInstanceStage>();

            //Act
            var state = flow.GetNextState(
                fakeState,
                stateFactory) as IInstallSoftwareOnInstanceStage;

            //Assert
            Assert.IsNotNull(state);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_FromInstallSoftwareOnInstanceState_ReturnsNull()
        {
            //Arrange
            var flow = new ProvisionInstanceStageFlow(
                "some-plan-id",
                new Dogger.Domain.Models.Instance()
                {
                    Name = "some-instance-name"
                });

            var serviceProvider = TestServiceProviderFactory.CreateUsingStartup();
            var stateFactory = new ProvisioningStageFactory(serviceProvider);

            var fakeState = Substitute.For<InstallSoftwareOnInstanceStage>();

            //Act
            var state = flow.GetNextState(
                fakeState,
                stateFactory);

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
            var stateFactory = new ProvisioningStageFactory(serviceProvider);

            var fakeState = Substitute.For<IProvisioningStage>();

            //Act
            var exception = await Assert.ThrowsExceptionAsync<UnknownFlowStageException>(async () => 
                flow.GetNextState(
                    fakeState,
                    stateFactory));

            //Assert
            Assert.IsNotNull(exception);
        }
    }
}
