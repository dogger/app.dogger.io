using System;
using System.Threading.Tasks;
using Dogger.Domain.Services.Provisioning.Flows;
using Dogger.Domain.Services.Provisioning.Stages;
using Dogger.Tests.TestHelpers;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Dogger.Tests.Domain.Services.Provisioning.Flows
{
    [TestClass]
    public class AggregateProvisioningStateFlowTest
    {
        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_CurrentFlowIsFirstFlow_ReturnsInitialStateOfFirstFlow()
        {
            //Arrange
            var context = new InitialStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>());

            var fakeProvisioningStateFlow = Substitute.For<IProvisioningStageFlow>();

            var fakeInitialState = await fakeProvisioningStateFlow.GetInitialState(context);

            var flow = new AggregateProvisioningStageFlow(fakeProvisioningStateFlow);

            //Act
            var initialState = await flow.GetInitialState(context);

            //Assert
            Assert.AreSame(fakeInitialState, initialState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetInitialState_CurrentFlowIsSecondFlow_ThrowsException()
        {
            //Arrange
            var initialStateContext = new InitialStateContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>());

            var nextStateContext = new NextStageContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningStage>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStageFlow>();
            fakeProvisioningStateFlow1
                .GetNextState(nextStateContext)
                .Returns((IProvisioningStage)null);

            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStageFlow>();

            var flow = new AggregateProvisioningStageFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            await flow.GetNextState(nextStateContext);

            //Act
            var exception = await Assert.ThrowsExceptionAsync<InvalidOperationException>(async () => 
                await flow.GetInitialState(initialStateContext));

            //Assert
            Assert.IsNotNull(exception);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFirstFlowAndNotFinished_ReturnsNextStateOfFirstFlow()
        {
            //Arrange
            var nextStateContext = new NextStageContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningStage>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStageFlow>();
            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStageFlow>();

            var fakeNextState = await fakeProvisioningStateFlow1.GetNextState(nextStateContext);

            var flow = new AggregateProvisioningStageFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            //Act
            var nextState = await flow.GetNextState(nextStateContext);

            //Assert
            Assert.AreSame(fakeNextState, nextState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFirstFlowAndFinished_ReturnsInitialStateOfSecondFlow()
        {
            //Arrange
            var nextStateContext = new NextStageContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningStage>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStageFlow>();
            fakeProvisioningStateFlow1
                .GetNextState(nextStateContext)
                .Returns((IProvisioningStage)null);

            var fakeProvisioningStateFlow2 = Substitute.For<IProvisioningStageFlow>();

            var fakeInitialSecondFlowState = Substitute.For<IProvisioningStage>();
            fakeProvisioningStateFlow2
                .GetInitialState(Arg.Any<InitialStateContext>())
                .Returns(fakeInitialSecondFlowState);

            var flow = new AggregateProvisioningStageFlow(
                fakeProvisioningStateFlow1,
                fakeProvisioningStateFlow2);

            //Act
            var nextState = await flow.GetNextState(nextStateContext);

            //Assert
            Assert.AreSame(fakeInitialSecondFlowState, nextState);
        }

        [TestMethod]
        [TestCategory(TestCategories.UnitCategory)]
        public async Task GetNextState_CurrentFlowIsFinalFlowAndFinished_ReturnsNull()
        {
            //Arrange
            var nextStateContext = new NextStageContext(
                Substitute.For<IMediator>(),
                Substitute.For<IProvisioningStateFactory>(),
                Substitute.For<IProvisioningStage>());

            var fakeProvisioningStateFlow1 = Substitute.For<IProvisioningStageFlow>();
            fakeProvisioningStateFlow1
                .GetNextState(nextStateContext)
                .Returns((IProvisioningStage)null);

            var flow = new AggregateProvisioningStageFlow(
                fakeProvisioningStateFlow1);

            //Act
            var nextState = await flow.GetNextState(nextStateContext);

            //Assert
            Assert.IsNull(nextState);
        }
    }
}
